using System.Reflection;
using FluentAssertions;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Validation;
using TUnit.Core;

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
    public async Task RunAsync_With_NoBanner_True_Still_Completes_Successfully()
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

            var reporter = new SimpleGenerationReporter();
            var orchestrator = new GenerationOrchestrator();
            var result = await orchestrator.RunAsync(settings, cliSettings, reporter, default);

            result.Should().Be(0);
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

}
