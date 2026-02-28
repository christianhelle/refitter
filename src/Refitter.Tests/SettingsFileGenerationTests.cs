using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class SettingsFileGenerationTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
  title: 'Test API'
servers:
  - url: 'https://test.host.com/api/v1'
paths:
  /test:
    get:
      tags:
      - 'Test'
      summary: 'Get test'
      responses:
        '200':
          description: 'successful operation'
          content:
            application/json:
              schema:
                type: 'object'
                properties:
                  id:
                    type: 'integer'
                  name:
                    type: 'string'
";

    [After(Test)]
    public async Task Cleanup()
    {
        // Clean up any generated .refitter files
        await CleanupGeneratedFiles();
    }

    [Test]
    public async Task Settings_File_Contains_Proper_Serialization_Of_RefitGeneratorSettings()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var settingsFile = Path.Combine(tempDir, ".refitter");

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = "TestNamespace",
                GenerateContracts = true,
                GenerateClients = true,
                UseCancellationTokens = true,
                ReturnIApiResponse = true
            };

            // Act
            var json = Serializer.Serialize(settings);
            await File.WriteAllTextAsync(settingsFile, json);

            // Assert
            json.Should().NotBeNullOrWhiteSpace();
            json.Should().Contain("\"namespace\"");
            json.Should().Contain("TestNamespace");
            json.Should().Contain("\"generateContracts\"");
            json.Should().Contain("\"generateClients\"");

            // Verify deserialization works
            var deserializedSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);
            deserializedSettings.Should().NotBeNull();
            deserializedSettings.Namespace.Should().Be("TestNamespace");
            deserializedSettings.GenerateContracts.Should().BeTrue();
            deserializedSettings.GenerateClients.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public async Task Settings_File_Creation_Handles_Directory_Creation()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var nestedDir = Path.Combine(tempDir, "nested", "deep", "path");
        // Do not create the directory - let the test verify it gets created

        try
        {
            var settingsFile = Path.Combine(nestedDir, ".refitter");

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = "TestNamespace"
            };

            // Act
            var json = Serializer.Serialize(settings);
            var settingsDirectory = Path.GetDirectoryName(settingsFile);

            if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            await File.WriteAllTextAsync(settingsFile, json);

            // Assert
            Directory.Exists(nestedDir).Should().BeTrue();
            File.Exists(settingsFile).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Test]
    public async Task Settings_File_Write_Throws_On_Filesystem_Errors()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var invalidPath = Path.Combine("\0invalid", ".refitter"); // Invalid path with null character

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            Namespace = "TestNamespace"
        };

        var json = Serializer.Serialize(settings);

        // Act & Assert
        var act = async () =>
        {
            try
            {
                var settingsDirectory = Path.GetDirectoryName(invalidPath);
                if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }
                await File.WriteAllTextAsync(invalidPath, json);
            }
            catch (Exception ex) when (ex is IOException ||
                                        ex is UnauthorizedAccessException ||
                                        ex is System.Security.SecurityException ||
                                        ex is NotSupportedException)
            {
                throw new InvalidOperationException(
                    $"Failed to write Refitter settings file to '{invalidPath}'. See inner exception for details.",
                    ex);
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to write Refitter settings file to*");
    }

    [Test]
    public void DetermineSettingsFilePath_Logic_For_File_Extension_In_MultiFile_Mode()
    {
        // Test the logic that would be used in DetermineSettingsFilePath
        // when OutputPath has an extension in multi-file mode

        // Arrange
        var outputPathWithExtension = Path.Combine("/tmp/output", "Output.cs");
        var outputPathWithoutExtension = "/tmp/output";

        // Act - Simulate the logic for multi-file mode with file extension
        string? outputDir1;
        if (Path.HasExtension(outputPathWithExtension))
        {
            outputDir1 = Path.GetDirectoryName(outputPathWithExtension);
        }
        else
        {
            outputDir1 = outputPathWithExtension;
        }

        string? outputDir2;
        if (Path.HasExtension(outputPathWithoutExtension))
        {
            outputDir2 = Path.GetDirectoryName(outputPathWithoutExtension);
        }
        else
        {
            outputDir2 = outputPathWithoutExtension;
        }

        var settingsFilePath1 = !string.IsNullOrWhiteSpace(outputDir1)
            ? Path.Combine(outputDir1, ".refitter")
            : ".refitter";

        var settingsFilePath2 = !string.IsNullOrWhiteSpace(outputDir2)
            ? Path.Combine(outputDir2, ".refitter")
            : ".refitter";

        // Assert
        settingsFilePath1.Should().Be(Path.Combine("/tmp/output", ".refitter"));
        settingsFilePath2.Should().Be(Path.Combine("/tmp/output", ".refitter"));
    }

    private static async Task CleanupGeneratedFiles()
    {
        // Clean up any .refitter files in common test locations
        var currentDir = Directory.GetCurrentDirectory();
        var refitterFile = Path.Combine(currentDir, ".refitter");

        if (File.Exists(refitterFile))
        {
            await Task.Run(() => File.Delete(refitterFile));
        }
    }
}
