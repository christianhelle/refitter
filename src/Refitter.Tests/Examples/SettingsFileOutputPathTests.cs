using System.Reflection;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;

namespace Refitter.Tests.Examples;

/// <summary>
/// Regression tests for issue #998: MSBuild + .NET 10 ignores outputFolder and filename-from-.refitter behavior
/// Tests exercise the actual GenerateCommand helper logic via reflection
/// </summary>
public class SettingsFileOutputPathTests
{
    private const string MinimalOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/test": {
              "get": {
                "operationId": "getTest",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "object",
                          "properties": { "id": { "type": "integer" } }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public void SettingsFile_WithoutOutputFilename_UsesRefitterFilenameFallback()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var settingsFile = Path.Combine(tempDir, "myapi.refitter");
            File.WriteAllText(settingsFile, "{}");

            var settingsJson = """
                {
                  "openApiPath": "spec.json",
                  "namespace": "TestNamespace"
                }
                """;

            var refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(settingsJson);
            var settings = new Settings { SettingsFilePath = settingsFile };

            ApplySettingsFileDefaults(settingsFile, refitGeneratorSettings);

            refitGeneratorSettings.OutputFilename.Should().Be("myapi.cs",
                "because OutputFilename should default to the .refitter filename");

            var outputPath = GetOutputPath(settings, refitGeneratorSettings);
            Path.GetFullPath(outputPath).Should().Be(Path.GetFullPath(Path.Combine(tempDir, "Generated", "myapi.cs")),
                "because default output folder is ./Generated rooted at the settings file directory");
        }
        finally
        {
            CleanupDirectory(tempDir);
        }
    }

    [Test]
    public void SettingsFile_WithExplicitDefaultOutputFolder_RootsToSettingsFileDirectory()
    {
        var tempDir = CreateTempDirectory();

        try
        {
            var settingsFile = Path.Combine(tempDir, "api.refitter");
            File.WriteAllText(settingsFile, "{}");

            var settingsJson = """
                {
                  "openApiPath": "spec.json",
                  "namespace": "TestNamespace",
                  "outputFolder": "./Generated",
                  "outputFilename": "Api.cs"
                }
                """;

            var refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(settingsJson);
            var settings = new Settings { SettingsFilePath = settingsFile };

            ApplySettingsFileDefaults(settingsFile, refitGeneratorSettings);

            var outputPath = GetOutputPath(settings, refitGeneratorSettings);
            Path.GetFullPath(outputPath).Should().Be(Path.GetFullPath(Path.Combine(tempDir, "Generated", "Api.cs")),
                "because outputFolder is rooted relative to the .refitter file directory");
        }
        finally
        {
            CleanupDirectory(tempDir);
        }
    }

    [Test]
    public async Task SettingsFile_PreservesNamingInterfaceName()
    {
        // Arrange: .refitter with custom naming.interfaceName
        var tempDir = CreateTempDirectory();

        try
        {
            var openApiFile = Path.Combine(tempDir, "spec.json");
            await File.WriteAllTextAsync(openApiFile, MinimalOpenApiSpec);

            var settingsJson = """
                {
                  "openApiPath": "spec.json",
                  "namespace": "TestNamespace",
                  "naming": {
                    "useOpenApiTitle": false,
                    "interfaceName": "CustomApi"
                  }
                }
                """;

            var refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(settingsJson);
            refitGeneratorSettings.OpenApiPath = openApiFile;

            var generator = await RefitGenerator.CreateAsync(refitGeneratorSettings);
            var code = generator.Generate();

            // Assert: Naming settings should be preserved and used in generation
            refitGeneratorSettings.Naming.Should().NotBeNull();
            refitGeneratorSettings.Naming.InterfaceName.Should().Be("CustomApi");
            code.Should().Contain("interface ICustomApi", "because custom interface name should be used");
            BuildHelper.BuildCSharp(code).Should().BeTrue();
        }
        finally
        {
            CleanupDirectory(tempDir);
        }
    }

    private static string GetOutputPath(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(Settings), typeof(RefitGeneratorSettings)],
            null);

        method.Should().NotBeNull();

        return (string)method!.Invoke(null, [settings, refitGeneratorSettings])!;
    }

    private static void ApplySettingsFileDefaults(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        var method = typeof(GenerateCommand).GetMethod(
            "ApplySettingsFileDefaults",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        method!.Invoke(null, [settingsFilePath, refitGeneratorSettings]);
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"refitter-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void CleanupDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
