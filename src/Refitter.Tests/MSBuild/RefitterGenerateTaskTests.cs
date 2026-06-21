using System.Reflection;
using FluentAssertions;
using Microsoft.Build.Framework;
using Refitter.MSBuild;

namespace Refitter.Tests.MSBuild;


[Category("Unit")]
public class RefitterGenerateTaskTests
{
    [Test]
    public void FilterFiles_Should_Require_Exact_File_Name_Match()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "petstore.refitter"),
            Path.Combine(projectRoot, "internal-petstore.refitter"),
            Path.Combine(projectRoot, "petstore.refitter.bak.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, "petstore.refitter", projectRoot);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "petstore.refitter"));
    }

    [Test]
    public void FilterFiles_Should_Match_Project_Relative_Path()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "samples", "petstore.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, @"apis\petstore.refitter", projectRoot);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "apis", "petstore.refitter"));
    }

    [Test]
    public void FilterFiles_Should_Match_Exact_Full_Path()
    {
        var workspace = CreateWorkspace();

        try
        {
            var projectRoot = workspace;
            var matchingFile = Path.Combine(projectRoot, "apis", "petstore.refitter");
            var files = new[]
            {
                matchingFile,
                Path.Combine(projectRoot, "apis", "petstore-v2.refitter")
            };

            var result = RefitterGenerateTask.FilterFiles(files, matchingFile, projectRoot);

            result.Should().ContainSingle().Which.Should().Be(matchingFile);
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void FilterFiles_Should_Not_Match_Substring_Project_Relative_Path()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "apis", "petstore-v2.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, @"apis\petstore", projectRoot);

        result.Should().BeEmpty();
    }

    [Test]
    public void FilterFiles_Should_Return_All_Files_When_IncludePatterns_Are_Empty()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "samples", "petstore.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, string.Empty, projectRoot);

        result.Should().Equal(files);
    }

    [Test]
    public void FilterFiles_Should_Handle_Blank_Project_File_Directory()
    {
        var matchingFile = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "petstore.refitter"));
        var files = new[]
        {
            matchingFile,
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "petstore-v2.refitter"))
        };

        var result = RefitterGenerateTask.FilterFiles(files, matchingFile, string.Empty);

        result.Should().ContainSingle().Which.Should().Be(matchingFile);
    }

    [Test]
    public void FilterFiles_Should_Normalize_Relative_Prefix_In_Include_Patterns()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "samples", "petstore.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, $".{Path.DirectorySeparatorChar}apis{Path.DirectorySeparatorChar}petstore.refitter", projectRoot);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "apis", "petstore.refitter"));
    }

    [Test]
    public void FilterFiles_Should_Handle_Project_Directory_With_Trailing_Separator()
    {
        var projectRoot = Path.Combine("C:", "repo");
        var files = new[]
        {
            Path.Combine(projectRoot, "apis", "petstore.refitter"),
            Path.Combine(projectRoot, "samples", "petstore.refitter")
        };

        var result = RefitterGenerateTask.FilterFiles(files, @"apis\petstore.refitter", projectRoot + Path.DirectorySeparatorChar);

        result.Should().ContainSingle().Which.Should().Be(Path.Combine(projectRoot, "apis", "petstore.refitter"));
    }

    [Test]
    public void Execute_Should_Generate_Files_Reported_By_Refitter()
    {
        var workspace = CreateWorkspace();

        try
        {
            var openApiPath = Path.Combine(workspace, "petstore.json");
            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": {
                    "title": "Petstore",
                    "version": "1.0.0"
                  },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "responses": {
                          "200": {
                            "description": "Success",
                            "content": {
                              "application/json": {
                                "schema": {
                                  "type": "array",
                                  "items": {
                                    "$ref": "#/components/schemas/Pet"
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  },
                  "components": {
                    "schemas": {
                      "Pet": {
                        "type": "object",
                        "properties": {
                          "id": {
                            "type": "integer",
                            "format": "int32"
                          }
                        }
                      }
                    }
                  }
                }
                """);

            var settingsPath = Path.Combine(workspace, "petstore.refitter");
            File.WriteAllText(
                settingsPath,
                """
                {
                  "openApiPath": "petstore.json",
                  "namespace": "Generated.Clients",
                  "outputFolder": "./Generated"
                }
                """);

            var task = new RefitterGenerateTask
            {
                BuildEngine = new RecordingBuildEngine(),
                ProjectFileDirectory = workspace,
                IncludePatterns = "petstore.refitter",
                DisableLogging = true,
                SkipValidation = true
            };

            var result = task.Execute();

            result.Should().BeTrue();
            task.GeneratedFiles.Should().ContainSingle();

            var generatedFile = task.GeneratedFiles.Single().ItemSpec;
            File.Exists(generatedFile).Should().BeTrue();
            File.ReadAllText(generatedFile).Should().Contain("interface IPetstore");
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Skip_Validation_When_SkipValidation_Is_True()
    {
        var workspace = CreateWorkspace();

        try
        {
            var openApiPath = Path.Combine(workspace, "petstore.json");
            File.WriteAllText(openApiPath, "not-valid-json");

            var settingsPath = Path.Combine(workspace, "petstore.refitter");
            File.WriteAllText(
                settingsPath,
                """{"openApiPath": "petstore.json", "namespace": "Test"}""");

            var task = new RefitterGenerateTask
            {
                BuildEngine = new RecordingBuildEngine(),
                ProjectFileDirectory = workspace,
                IncludePatterns = "petstore.refitter",
                SkipValidation = true
            };

            var result = task.Execute();

            result.Should().BeFalse();
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Generate_Multiple_Files()
    {
        var workspace = CreateWorkspace();

        try
        {
            var openApiPath = Path.Combine(workspace, "petstore.json");
            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Petstore", "version": "1.0.0" },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "tags": ["pets"],
                        "responses": { "200": { "description": "ok" } }
                      }
                    }
                  }
                }
                """);

            var settingsPath = Path.Combine(workspace, "petstore.refitter");
            File.WriteAllText(
                settingsPath,
                """
                {
                  "openApiPath": "petstore.json",
                  "namespace": "Test",
                  "generateMultipleFiles": true,
                  "outputFolder": "./Generated"
                }
                """);

            var task = new RefitterGenerateTask
            {
                BuildEngine = new RecordingBuildEngine(),
                ProjectFileDirectory = workspace,
                IncludePatterns = "petstore.refitter",
                SkipValidation = true
            };

            var result = task.Execute();

            result.Should().BeTrue();
            task.GeneratedFiles.Should().NotBeEmpty();
            task.GeneratedFiles.Length.Should().BeGreaterThan(1);
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Apply_SettingsFileDefaults()
    {
        var workspace = CreateWorkspace();

        try
        {
            var openApiPath = Path.Combine(workspace, "petstore.json");
            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Petstore", "version": "1.0.0" },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "responses": { "200": { "description": "ok" } }
                      }
                    }
                  }
                }
                """);

            var settingsPath = Path.Combine(workspace, "petstore.refitter");
            File.WriteAllText(
                settingsPath,
                """{"openApiPath": "petstore.json", "namespace": "Test"}""");

            var task = new RefitterGenerateTask
            {
                BuildEngine = new RecordingBuildEngine(),
                ProjectFileDirectory = workspace,
                IncludePatterns = "petstore.refitter",
                SkipValidation = true
            };

            var result = task.Execute();

            result.Should().BeTrue();
            task.GeneratedFiles.Should().ContainSingle();

            // Default outputFolder should be ./Generated and filename from settings file name
            var generatedPath = task.GeneratedFiles.Single().ItemSpec;
            generatedPath.Should().Contain("Generated");
            File.Exists(generatedPath).Should().BeTrue();
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Respect_ContractsOutputFolder()
    {
        var workspace = CreateWorkspace();

        try
        {
            var openApiPath = Path.Combine(workspace, "petstore.json");
            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Petstore", "version": "1.0.0" },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "tags": ["pets"],
                        "responses": { "200": { "description": "ok" } }
                      }
                    }
                  }
                }
                """);

            var settingsPath = Path.Combine(workspace, "petstore.refitter");
            File.WriteAllText(
                settingsPath,
                """
                {
                  "openApiPath": "petstore.json",
                  "namespace": "Test",
                  "generateMultipleFiles": true,
                  "outputFolder": "./Generated",
                  "contractsOutputFolder": "./Contracts"
                }
                """);

            var task = new RefitterGenerateTask
            {
                BuildEngine = new RecordingBuildEngine(),
                ProjectFileDirectory = workspace,
                IncludePatterns = "petstore.refitter",
                SkipValidation = true
            };

            var result = task.Execute();

            result.Should().BeTrue();
            var allGenerated = task.GeneratedFiles.Select(f => f.ItemSpec).ToArray();
            allGenerated.Should().Contain(f => f.Contains("Contracts"));
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void TryLogCommandLine_Should_Swallow_BuildEngine_Exceptions()
    {
        var task = new RefitterGenerateTask { BuildEngine = new RecordingBuildEngine { ThrowOnMessages = true } };
        var action = () => InvokePrivateMethod(task, "TryLogCommandLine", "message");

        action.Should().NotThrow();
    }

    [Test]
    public void TryLogError_Should_Swallow_BuildEngine_Exceptions()
    {
        var task = new RefitterGenerateTask { BuildEngine = new RecordingBuildEngine { ThrowOnErrors = true } };
        var action = () => InvokePrivateMethod(task, "TryLogError", "message");

        action.Should().NotThrow();
    }

    [Test]
    public void TryLogErrorFromException_Should_Swallow_BuildEngine_Exceptions()
    {
        var task = new RefitterGenerateTask { BuildEngine = new RecordingBuildEngine { ThrowOnErrors = true } };
        var action = () => InvokePrivateMethod(task, "TryLogErrorFromException", new InvalidOperationException("boom"));

        action.Should().NotThrow();
    }

    [Test]
    public void TryLogErrorFromException_Should_Log_When_BuildEngine_Allows_It()
    {
        var buildEngine = new RecordingBuildEngine();
        var task = new RefitterGenerateTask { BuildEngine = buildEngine };

        InvokePrivateMethod(task, "TryLogErrorFromException", new InvalidOperationException("boom"));

        buildEngine.Errors.Should().Contain(message => message.Contains("boom", StringComparison.Ordinal));
    }

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(AppContext.BaseDirectory, "RefitterGenerateTaskTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void InvokePrivateMethod(RefitterGenerateTask task, string methodName, params object[] arguments)
    {
        var method = typeof(RefitterGenerateTask).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(task, arguments);
    }

    private static void DeleteWorkspace(string workspace)
    {
        if (Directory.Exists(workspace))
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private sealed class RecordingBuildEngine : IBuildEngine
    {
        public List<string> Messages { get; } = [];
        public List<string> Errors { get; } = [];
        public bool ThrowOnMessages { get; init; }
        public bool ThrowOnErrors { get; init; }

        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            if (ThrowOnMessages)
            {
                throw new InvalidOperationException("custom event logging failed");
            }

            if (e.Message is not null)
            {
                Messages.Add(e.Message);
            }
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            if (ThrowOnErrors)
            {
                throw new InvalidOperationException("error logging failed");
            }

            if (e.Message is not null)
            {
                Errors.Add(e.Message);
            }
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            if (ThrowOnMessages)
            {
                throw new InvalidOperationException("message logging failed");
            }

            if (e.Message is not null)
            {
                Messages.Add(e.Message);
            }
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            if (e.Message is not null)
            {
                Messages.Add(e.Message);
            }
        }
    }
}
