using FluentAssertions;
using Microsoft.Build.Framework;
using Refitter.MSBuild;

namespace Refitter.Tests;

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
    public void ParseGeneratedFilePath_Should_Return_File_Path_From_Marker()
    {
        var generatedFile = Path.Combine("C:", "repo", "Generated", "Petstore.cs");

        var result = RefitterGenerateTask.ParseGeneratedFilePath($"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile}");

        result.Should().Be(generatedFile);
    }

    [Test]
    public void ParseGeneratedFilePath_Should_Return_Null_For_Empty_Marker_Path()
    {
        var result = RefitterGenerateTask.ParseGeneratedFilePath(RefitterGenerateTask.GeneratedFileMarker);

        result.Should().BeNull();
    }

    [Test]
    public void ParseGeneratedFilePath_Should_Return_Null_For_Whitespace_Output()
    {
        var result = RefitterGenerateTask.ParseGeneratedFilePath("   ");

        result.Should().BeNull();
    }

    [Test]
    public void ParseGeneratedFilePath_Should_Ignore_Non_Marker_Output()
    {
        var result = RefitterGenerateTask.ParseGeneratedFilePath("Generated Output");

        result.Should().BeNull();
    }

    [Test]
    public void HandleProcessErrorOutput_Should_Ignore_Whitespace()
    {
        var messages = new List<string>();

        RefitterGenerateTask.HandleProcessErrorOutput("  ", messages.Add);

        messages.Should().BeEmpty();
    }

    [Test]
    public void HandleProcessErrorOutput_Should_Log_Non_Whitespace()
    {
        var messages = new List<string>();

        RefitterGenerateTask.HandleProcessErrorOutput("stderr output", messages.Add);

        messages.Should().ContainSingle().Which.Should().Be("stderr output");
    }

    [Test]
    public void HandleProcessStandardOutput_Should_Ignore_Whitespace()
    {
        var outputLines = new List<string>();
        var logged = new List<string>();

        RefitterGenerateTask.HandleProcessStandardOutput(" ", outputLines, new object(), logged.Add);

        outputLines.Should().BeEmpty();
        logged.Should().BeEmpty();
    }

    [Test]
    public void HandleProcessStandardOutput_Should_Record_And_Log_Output()
    {
        var outputLines = new List<string>();
        var logged = new List<string>();

        RefitterGenerateTask.HandleProcessStandardOutput("Generated output", outputLines, new object(), logged.Add);

        outputLines.Should().ContainSingle().Which.Should().Be("Generated output");
        logged.Should().ContainSingle().Which.Should().Be("Generated output");
    }

    [Test]
    public void ResolveGeneratedFiles_Should_Deduplicate_Duplicate_Markers()
    {
        var workspace = CreateWorkspace();

        try
        {
            var generatedFile = Path.Combine(workspace, "Generated", "Petstore.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(generatedFile)!);
            File.WriteAllText(generatedFile, "// generated");

            var outputLines = new[]
            {
                "Generated Output",
                $"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile}",
                $"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile.ToUpperInvariant()}"
            };

            var result = RefitterGenerateTask.ResolveGeneratedFiles(outputLines, "petstore.refitter", out var errorMessage);

            result.Should().ContainSingle().Which.Should().Be(generatedFile);
            errorMessage.Should().BeNull();
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void ResolveGeneratedFiles_Should_Fail_When_No_Markers_Are_Reported()
    {
        var outputLines = new[]
        {
            "Generating code...",
            "Generation completed successfully!"
        };

        var result = RefitterGenerateTask.ResolveGeneratedFiles(outputLines, "petstore.refitter", out var errorMessage);

        result.Should().BeEmpty();
        errorMessage.Should().Be("Refitter did not report any generated files for petstore.refitter");
    }

    [Test]
    public void ResolveGeneratedFiles_Should_Log_Error_And_Set_Failed_When_No_Markers_Are_Reported()
    {
        var outputLines = new[]
        {
            "Generating code...",
            "Generation completed successfully!"
        };
        var errors = new List<string>();

        var result = RefitterGenerateTask.ResolveGeneratedFiles(outputLines, "petstore.refitter", out var failed, errors.Add);

        result.Should().BeEmpty();
        failed.Should().BeTrue();
        errors.Should().ContainSingle().Which.Should().Be("Refitter did not report any generated files for petstore.refitter");
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
                BuildEngine = new FakeBuildEngine(),
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

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(AppContext.BaseDirectory, "RefitterGenerateTaskTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void DeleteWorkspace(string workspace)
    {
        if (Directory.Exists(workspace))
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private sealed class FakeBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
        }
    }
}
