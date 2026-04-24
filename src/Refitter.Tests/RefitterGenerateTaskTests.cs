using System.Diagnostics;
using System.Reflection;
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
    public void ParseGeneratedFilePath_Should_Return_Null_For_Null_Output()
    {
        var result = RefitterGenerateTask.ParseGeneratedFilePath(null);

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
    public void Execute_Should_Return_False_When_Runtime_Discovery_Throws()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => throw new InvalidOperationException("boom");

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeFalse();
            task.GeneratedFiles.Should().BeEmpty();
            buildEngine.Errors.Should().Contain(message => message.Contains("Failed to generate code from", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Use_DotNet9_Runtime_When_Available()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            var generatedFile = CreateGeneratedFile(workspace);

            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => ["Microsoft.NETCore.App 9.0.0"];
            RefitterGenerateTask.ProcessRunner = (startInfo, logOutput, _) =>
            {
                startInfo.Arguments.Should().Contain("net9.0");
                logOutput($"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile}");
                return new RefitterGenerateTask.ProcessExecutionResult(false, 0);
            };

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeTrue();
            task.GeneratedFiles.Should().ContainSingle();
            task.GeneratedFiles.Single().ItemSpec.Should().Be(generatedFile);
            buildEngine.Messages.Should().Contain(message => message.Contains(".NET 9 runtime", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Fall_Back_To_DotNet8_Runtime_When_Newer_Runtimes_Are_Unavailable()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            var generatedFile = CreateGeneratedFile(workspace);

            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => ["Microsoft.NETCore.App 8.0.0"];
            RefitterGenerateTask.ProcessRunner = (startInfo, logOutput, _) =>
            {
                startInfo.Arguments.Should().Contain("net8.0");
                logOutput($"{RefitterGenerateTask.GeneratedFileMarker}{generatedFile}");
                return new RefitterGenerateTask.ProcessExecutionResult(false, 0);
            };

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeTrue();
            task.GeneratedFiles.Should().ContainSingle();
            buildEngine.Messages.Should().Contain(message => message.Contains("Using .NET 8 version of Refitter.", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Log_Timeout_When_Process_Does_Not_Exit()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => ["Microsoft.NETCore.App 10.0.0"];
            RefitterGenerateTask.ProcessRunner = (_, _, _) => new RefitterGenerateTask.ProcessExecutionResult(true, -1);

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeFalse();
            task.GeneratedFiles.Should().BeEmpty();
            buildEngine.Errors.Should().Contain(message => message.Contains("timed out after 300 seconds", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Log_When_Timed_Out_Process_Cannot_Be_Terminated()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => ["Microsoft.NETCore.App 10.0.0"];
            RefitterGenerateTask.ProcessRunner = (_, _, _) => new RefitterGenerateTask.ProcessExecutionResult(
                true,
                -1,
                new InvalidOperationException("termination failed"));

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeFalse();
            buildEngine.Errors.Should().Contain(message => message.Contains("Failed to terminate timed-out process: termination failed", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Execute_Should_Log_When_Process_Exits_With_Non_Zero_Code()
    {
        var workspace = CreateWorkspace();

        try
        {
            CreateRefitterSettingsFile(workspace);
            RefitterGenerateTask.InstalledDotnetRuntimesProvider = () => ["Microsoft.NETCore.App 10.0.0"];
            RefitterGenerateTask.ProcessRunner = (_, _, _) => new RefitterGenerateTask.ProcessExecutionResult(false, 23);

            var buildEngine = new RecordingBuildEngine();
            var task = CreateTask(workspace, buildEngine);

            var result = task.Execute();

            result.Should().BeFalse();
            buildEngine.Errors.Should().Contain(message => message.Contains("Refitter process exited with code 23", StringComparison.Ordinal));
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void RunProcess_Should_Return_TimedOut_Result_When_Process_Exceeds_Timeout()
    {
        try
        {
            RefitterGenerateTask.ProcessTimeoutMilliseconds = 1;

            var result = InvokeRunProcess(CreateSleepProcessStartInfo());

            result.TimedOut.Should().BeTrue();
            result.TerminationException.Should().BeNull();
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
        }
    }

    [Test]
    public void RunProcess_Should_Return_Termination_Exception_When_Kill_Fails_After_Timeout()
    {
        try
        {
            RefitterGenerateTask.ProcessTimeoutMilliseconds = 1;
            RefitterGenerateTask.ProcessTerminator = _ => throw new InvalidOperationException("kill failed");

            var result = InvokeRunProcess(CreateSleepProcessStartInfo());

            result.TimedOut.Should().BeTrue();
            result.TerminationException.Should().BeOfType<InvalidOperationException>()
                .Which.Message.Should().Be("kill failed");
        }
        finally
        {
            RefitterGenerateTask.ResetTestHooks();
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

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(AppContext.BaseDirectory, "RefitterGenerateTaskTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static string CreateRefitterSettingsFile(string workspace)
    {
        var settingsPath = Path.Combine(workspace, "petstore.refitter");
        File.WriteAllText(settingsPath, "{}");
        return settingsPath;
    }

    private static string CreateGeneratedFile(string workspace)
    {
        var generatedFile = Path.Combine(workspace, "Generated", "Petstore.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(generatedFile)!);
        File.WriteAllText(generatedFile, "// generated");
        return generatedFile;
    }

    private static RefitterGenerateTask CreateTask(string workspace, IBuildEngine? buildEngine = null) =>
        new()
        {
            BuildEngine = buildEngine ?? new RecordingBuildEngine(),
            ProjectFileDirectory = workspace,
            IncludePatterns = "petstore.refitter",
            DisableLogging = true,
            SkipValidation = true
        };

    private static void InvokePrivateMethod(RefitterGenerateTask task, string methodName, params object[] arguments)
    {
        var method = typeof(RefitterGenerateTask).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(task, arguments);
    }

    private static RefitterGenerateTask.ProcessExecutionResult InvokeRunProcess(ProcessStartInfo startInfo)
    {
        var method = typeof(RefitterGenerateTask).GetMethod("RunProcess", BindingFlags.Static | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return method!.Invoke(null, [startInfo, (Action<string?>)(_ => { }), (Action<string?>)(_ => { })])
            .Should()
            .BeOfType<RefitterGenerateTask.ProcessExecutionResult>()
            .Subject;
    }

    private static ProcessStartInfo CreateSleepProcessStartInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoLogo -NoProfile -Command \"Start-Sleep -Seconds 1\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }

        return new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "-lc \"sleep 1\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
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
