using System.Reflection;
using FluentAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

public class GenerationOrchestratorTests
{
    [Test]
    public async Task RunAsync_Should_Generate_Code_And_Return_Zero()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
            var generatedCode = await File.ReadAllTextAsync(outputPath);
            generatedCode.Should().Contain("TestNamespace");
            generatedCode.Should().Contain("GetPets");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Generate_Multiple_Files_And_Return_Zero()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputDir = Path.Combine(workspace, "Generated");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateMultipleFiles = true,
                OutputFolder = outputDir,
                GenerateContracts = false,
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputDir,
                GenerateMultipleFiles = true,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            var generatedFiles = Directory.GetFiles(outputDir, "*.cs");
            generatedFiles.Should().NotBeEmpty();
            var generatedCode = await File.ReadAllTextAsync(generatedFiles[0]);
            generatedCode.Should().Contain("TestNamespace");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Return_NonZero_On_Exception()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "nonexistent.json");
            Directory.CreateDirectory(workspace);

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().NotBe(0);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_NoBanner_False_Still_Completes_Successfully()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = false,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_MultipleOpenApiPaths_Completes_Successfully()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath1 = Path.Combine(workspace, "spec1.json");
            var openApiPath2 = Path.Combine(workspace, "spec2.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            var spec = """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "responses": { "200": { "description": "ok" } }
                      }
                    }
                  }
                }
                """;

            File.WriteAllText(openApiPath1, spec);
            File.WriteAllText(openApiPath2, spec);

            var settings = new RefitGeneratorSettings
            {
                OpenApiPaths = [openApiPath1, openApiPath2],
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath1,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_UseIsoDateFormat_And_DateFormat_Shows_Warning()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
                UseIsoDateFormat = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    DateFormat = "dd/MM/yyyy"
                }
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new TestGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();

            reporter.ConfigurationWarnings.Should().ContainSingle();
            reporter.ConfigurationWarnings[0].Title.Should().Be("Date Format Override");
            reporter.ConfigurationWarnings[0].Description.Should().Contain("useIsoDateFormat");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_Deprecated_UsePolly_Shows_Warning()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
#pragma warning disable CS0618
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    UsePolly = true
                },
#pragma warning restore CS0618
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new TestGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();

            reporter.ConfigurationWarnings.Should().ContainSingle();
            reporter.ConfigurationWarnings[0].Title.Should().Be("Deprecated Setting");
            reporter.ConfigurationWarnings[0].Description.Should().Contain("usePolly");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_IncludePathMatches_NoMatch_Shows_Warning()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
                IncludePathMatches = ["^/nonexistent$"],
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new TestGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            reporter.AllPathsFilteredWarningCalled.Should().BeTrue();
            reporter.AllPathsFilteredMatchPatterns.Should().ContainSingle()
                .Which.Should().Be("^/nonexistent$");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public void FormatFileSize_Small_Values()
    {
        var method = typeof(GenerationOrchestrator).GetMethod(
            "FormatFileSize",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(long)],
            null);

        method.Should().NotBeNull();

        var bytesResult = (string)method!.Invoke(null, [0L])!;
        bytesResult.Should().Match("0* B");

        var kbResult = (string)method.Invoke(null, [1024L])!;
        kbResult.Should().Match("1* KB");

        var mbResult = (string)method.Invoke(null, [1048576L])!;
        mbResult.Should().Match("1* MB");

        var gbResult = (string)method.Invoke(null, [1073741824L])!;
        gbResult.Should().Match("1* GB");
    }

    [Test]
    public void FormatFileSize_Exact_Boundaries()
    {
        var method = typeof(GenerationOrchestrator).GetMethod(
            "FormatFileSize",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(long)],
            null);

        method.Should().NotBeNull();

        var justUnder1K = (string)method!.Invoke(null, [1023L])!;
        justUnder1K.Should().Match("1023* B");

        var exactly1K = (string)method.Invoke(null, [1024L])!;
        exactly1K.Should().Match("1* KB");

        var justOver1K = (string)method.Invoke(null, [1025L])!;
        justOver1K.Should().Match("1* KB");

        var justUnder1M = (string)method.Invoke(null, [1048575L])!;
        justUnder1M.Should().Match("1024* KB");

        var exactly1M = (string)method.Invoke(null, [1048576L])!;
        exactly1M.Should().Match("1* MB");
    }

    [Test]
    public async Task RunAsync_With_SkipValidation_False_And_Valid_Spec_Succeeds()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = false,
            };

            var reporter = new CapturingGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_Both_Warnings_Displayed()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);

            File.WriteAllText(
                openApiPath,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
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

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
                UseIsoDateFormat = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    DateFormat = "dd/MM/yyyy"
                },
#pragma warning disable CS0618
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    UsePolly = true
                },
#pragma warning restore CS0618
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new TestGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();

            reporter.ConfigurationWarnings.Should().HaveCount(2);
            reporter.ConfigurationWarnings.Should().Contain(w => w.Title == "Date Format Override");
            reporter.ConfigurationWarnings.Should().Contain(w => w.Title == "Deprecated Setting");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_SkipValidation_False_And_Non_Existent_File_Shows_SkipValidation_Suggestion()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var openApiPath = Path.Combine(workspace, "nonexistent.json");
            Directory.CreateDirectory(workspace);

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
            };

            var cliSettings = new Settings
            {
                OpenApiPath = openApiPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = false,
            };

            var reporter = new CapturingGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().NotBe(0);
            reporter.GenerationFailedCalled.Should().BeTrue();
            reporter.ExceptionDetailsCalled.Should().BeTrue();
            reporter.SkipValidationSuggestionCalled.Should().BeTrue();
            reporter.SupportHelpCalled.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_SettingsFilePath_Resolves_Paths_And_Does_Not_Write_Settings_File()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "GenerationOrchestratorTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var settingsDir = Path.Combine(workspace, "settings");
            var openApiPath = Path.Combine(settingsDir, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            var settingsFilePath = Path.Combine(settingsDir, ".refitter");
            Directory.CreateDirectory(settingsDir);

            var spec = """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
                  "paths": {
                    "/pets": {
                      "get": {
                        "operationId": "GetPets",
                        "responses": { "200": { "description": "ok" } }
                      }
                    }
                  }
                }
                """;

            File.WriteAllText(openApiPath, spec);
            File.WriteAllText(
                settingsFilePath,
                """{"openApiPath": "spec.json", "namespace": "TestNamespace"}""");

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var cliSettings = new Settings
            {
                SettingsFilePath = settingsFilePath,
                OpenApiPath = openApiPath,
                OutputPath = outputPath,
                NoLogging = true,
                NoBanner = true,
                SkipValidation = true,
            };

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
            File.Exists(outputPath).Should().BeTrue();

            // The .refitter settings file should not have been written because SettingsFilePath is set
            var refitterFilesInWorkspace = Directory.GetFiles(workspace, "*.refitter", SearchOption.AllDirectories);
            refitterFilesInWorkspace.Length.Should().Be(1);
            refitterFilesInWorkspace[0].Should().Be(settingsFilePath);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    /// <summary>
    /// Test implementation of IGenerationReporter that captures warnings for verification.
    /// </summary>
    private sealed class TestGenerationReporter : IGenerationReporter
    {
        public List<Warning> ConfigurationWarnings { get; } = [];
        public bool AllPathsFilteredWarningCalled { get; private set; }
        public IReadOnlyList<string> AllPathsFilteredMatchPatterns { get; private set; } = [];

        public void ReportHeader(string version) { }
        public void ReportSupportKey(string supportKey) { }
        public Task ReportSingleFileGenerationProgressAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines) { }
        public Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(Func<GeneratorOutput> generate, CancellationToken cancellationToken = default) => Task.FromResult(generate());
        public IMultiFileOutputReport BeginMultiFileOutput() => new TestMultiFileOutputReport();
        public void ReportFileWritten(string outputPath) { }
        public async Task<OpenApiValidationResult> ValidateWithProgressAsync(Func<Task<OpenApiValidationResult>> validate, CancellationToken cancellationToken = default) => await validate();
        public void ReportValidationFailed() { }
        public void ReportValidationDiagnostic(OpenApiError error, bool isError) { }
        public void ReportValidationStatistics(OpenApiValidationResult validationResult) { }
        public void ReportSuccess(TimeSpan duration, bool multipleFiles) { }
        public void ReportDonationBanner() { }

        public void ReportConfigurationWarnings(IReadOnlyList<Warning> warnings)
        {
            ConfigurationWarnings.AddRange(warnings);
        }

        public void ReportAllPathsFilteredWarning(IReadOnlyList<string> matchPatterns)
        {
            AllPathsFilteredWarningCalled = true;
            AllPathsFilteredMatchPatterns = matchPatterns;
        }

        public void ReportSettingsFileGenerated(string settingsFilePath) { }
        public void ReportGenerationFailed() { }
        public void ReportUnsupportedVersion(string specificationVersion) { }
        public void ReportExceptionDetails(Exception exception) { }
        public void ReportSkipValidationSuggestion() { }
        public void ReportSupportHelp() { }

        private sealed class TestMultiFileOutputReport : IMultiFileOutputReport
        {
            public void AddFile(string fileName, string directory, string sizeFormatted, int lines) { }
            public void Complete(int fileCount, string totalSizeFormatted, int totalLines) { }
        }
    }

    /// <summary>
    /// Test implementation of IGenerationReporter that captures all calls for verification.
    /// </summary>
    private sealed class CapturingGenerationReporter : IGenerationReporter
    {
        public string? SupportKey { get; private set; }
        public bool GenerationFailedCalled { get; private set; }
        public bool ExceptionDetailsCalled { get; private set; }
        public bool SkipValidationSuggestionCalled { get; private set; }
        public bool SupportHelpCalled { get; private set; }
        public bool ValidationFailedCalled { get; private set; }
        public string? UnsupportedVersion { get; private set; }
        public bool DonationBannerCalled { get; private set; }
        public List<Warning> ConfigurationWarnings { get; } = [];

        public void ReportHeader(string version) { }
        public void ReportSupportKey(string supportKey) => SupportKey = supportKey;
        public Task ReportSingleFileGenerationProgressAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void ReportSingleFileOutput(string fileName, string directory, string sizeFormatted, int lines) { }

        public Task<GeneratorOutput> GenerateMultipleFilesWithProgressAsync(Func<GeneratorOutput> generate, CancellationToken cancellationToken = default) =>
            Task.FromResult(generate());

        public IMultiFileOutputReport BeginMultiFileOutput() => new TestMultiFileOutputReport();
        public void ReportFileWritten(string outputPath) { }

        public async Task<OpenApiValidationResult> ValidateWithProgressAsync(
            Func<Task<OpenApiValidationResult>> validate,
            CancellationToken cancellationToken = default) => await validate();

        public void ReportValidationFailed() => ValidationFailedCalled = true;

        public void ReportValidationDiagnostic(OpenApiError error, bool isError) { }

        public void ReportValidationStatistics(OpenApiValidationResult validationResult) { }

        public void ReportSuccess(TimeSpan duration, bool multipleFiles) { }
        public void ReportDonationBanner() => DonationBannerCalled = true;

        public void ReportConfigurationWarnings(IReadOnlyList<Warning> warnings) =>
            ConfigurationWarnings.AddRange(warnings);

        public void ReportAllPathsFilteredWarning(IReadOnlyList<string> matchPatterns) { }
        public void ReportSettingsFileGenerated(string settingsFilePath) { }
        public void ReportGenerationFailed() => GenerationFailedCalled = true;
        public void ReportUnsupportedVersion(string specificationVersion) => UnsupportedVersion = specificationVersion;

        public void ReportExceptionDetails(Exception exception) => ExceptionDetailsCalled = true;

        public void ReportSkipValidationSuggestion() => SkipValidationSuggestionCalled = true;
        public void ReportSupportHelp() => SupportHelpCalled = true;

        private sealed class TestMultiFileOutputReport : IMultiFileOutputReport
        {
            public void AddFile(string fileName, string directory, string sizeFormatted, int lines) { }
            public void Complete(int fileCount, string totalSizeFormatted, int totalLines) { }
        }
    }
}
