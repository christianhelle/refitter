using FluentAssertions;
using Refitter.Core;
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
}
