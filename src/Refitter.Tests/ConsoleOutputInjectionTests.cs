using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

/// <summary>
/// End-to-end regression for GHSA-4j78-2jpc-xg6w: OpenAPI text must not reach stdout
/// as a CI logging command.
/// </summary>
[TestClass]
[DoNotParallelize]
public class ConsoleOutputInjectionTests
{
    private const string InjectedSpec =
        """
        {
          "openapi": "3.0.3",
          "info": { "title": "Sample API", "version": "1.0.0" },
          "paths": {
            "/probe": {
              "post": {
                "operationId": "Probe",
                "requestBody": {
                  "required": false,
                  "content": {
                    "application/json\r\n##vso[task.uploadfile]/work/workspace-data-a.txt\r\n": {
                      "schema": { "type": "string" }
                    }
                  }
                },
                "responses": { "200": { "description": "ok" } }
              }
            }
          }
        }
        """;

    private const string ValidSpec =
        """
        {
          "openapi": "3.0.3",
          "info": { "title": "Sample API", "version": "1.0.0" },
          "paths": {
            "/probe": {
              "post": {
                "operationId": "Probe",
                "requestBody": {
                  "required": false,
                  "content": {
                    "application/json": {
                      "schema": { "type": "string" }
                    }
                  }
                },
                "responses": { "200": { "description": "ok" } }
              }
            }
          }
        }
        """;

    [TestMethod]
    public async Task Simple_Output_Does_Not_Emit_Pipeline_Command_From_Spec_Text()
    {
        var (exitCode, output) = await RunSimpleAsync(InjectedSpec);

        output.Should().Contain("illegal characters", "the validation error must be reported");
        output.Contains("##vso[", StringComparison.OrdinalIgnoreCase).Should().BeFalse(output);
        output.Split('\n').Should().NotContain(line => line.Contains("##vso", StringComparison.OrdinalIgnoreCase), output);
        exitCode.Should().NotBe(0);
    }

    [TestMethod]
    public async Task Simple_Output_For_Valid_Spec_Succeeds()
    {
        var (exitCode, _) = await RunSimpleAsync(ValidSpec);

        exitCode.Should().Be(0);
    }

    private static async Task<(int ExitCode, string Output)> RunSimpleAsync(string spec)
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "ConsoleOutputInjectionTests",
            Guid.NewGuid().ToString("N"));

        var originalOut = Console.Out;
        var capture = new StringWriter();
        try
        {
            var openApiPath = Path.Combine(workspace, "spec.json");
            var outputPath = Path.Combine(workspace, "Output.cs");
            Directory.CreateDirectory(workspace);
            File.WriteAllText(openApiPath, spec);

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
                SimpleOutput = true,
            };

            Console.SetOut(capture);
            var exitCode = await GenerationOrchestrator.RunAsync(
                settings,
                cliSettings,
                new SimpleGenerationReporter(),
                default);

            return (exitCode, capture.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }
}
