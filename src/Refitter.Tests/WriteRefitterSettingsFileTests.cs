using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class WriteRefitterSettingsFileTests
{
    [Test]
    public void DetermineSettingsFilePath_Returns_Default_Path_When_No_Output_Path_Specified()
    {
        // Arrange
        var settings = new Settings
        {
            OutputPath = Settings.DefaultOutputPath
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(".refitter");
    }

    [Test]
    public void DetermineSettingsFilePath_Returns_Custom_Path_When_Output_Path_Is_File()
    {
        // Arrange
        var customPath = Path.Combine("custom", "output", "MyClient.cs");
        var expectedDir = Path.Combine("custom", "output");
        var settings = new Settings
        {
            OutputPath = customPath
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(Path.Combine(expectedDir, ".refitter"));
    }

    [Test]
    public void DetermineSettingsFilePath_Returns_Custom_Path_When_Output_Path_Is_Directory_And_GenerateMultipleFiles()
    {
        // Arrange
        var customPath = Path.Combine("custom", "output");
        var settings = new Settings
        {
            OutputPath = customPath,
            GenerateMultipleFiles = true
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(Path.Combine(customPath, ".refitter"));
    }

    [Test]
    public void DetermineSettingsFilePath_Returns_Custom_Path_When_ContractsOutputPath_Is_Set()
    {
        // Arrange
        var customPath = Path.Combine("custom", "output");
        var contractsPath = Path.Combine("custom", "contracts");
        var settings = new Settings
        {
            OutputPath = customPath,
            ContractsOutputPath = contractsPath
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(Path.Combine(customPath, ".refitter"));
    }

    [Test]
    public void DetermineSettingsFilePath_Returns_Default_When_Output_Path_Is_Empty()
    {
        // Arrange
        var settings = new Settings
        {
            OutputPath = ""
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(".refitter");
    }

    [Test]
    public void DetermineSettingsFilePath_Returns_Default_When_Output_Path_Is_Null()
    {
        // Arrange
        var settings = new Settings
        {
            OutputPath = null
        };

        // Act
        var result = GenerateCommand.DetermineSettingsFilePath(settings);

        // Assert
        result.Should().Be(".refitter");
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Creates_File_With_Default_Path()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var settings = new Settings
            {
                OutputPath = Settings.DefaultOutputPath,
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "TestNamespace"
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            var settingsFile = Path.Combine(tempDir, ".refitter");
            File.Exists(settingsFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(settingsFile);
            content.Should().NotBeNullOrWhiteSpace();
            content.Should().Contain("TestNamespace");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Creates_File_With_Custom_Output_Path()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputDir = Path.Combine(tempDir, "output");
            var settings = new Settings
            {
                OutputPath = Path.Combine(outputDir, "MyClient.cs"),
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "CustomNamespace"
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            var settingsFile = Path.Combine(outputDir, ".refitter");
            File.Exists(settingsFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(settingsFile);
            content.Should().Contain("CustomNamespace");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Creates_File_When_GenerateMultipleFiles_Is_True()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputDir = Path.Combine(tempDir, "output");
            var settings = new Settings
            {
                OutputPath = outputDir,
                GenerateMultipleFiles = true,
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "MultipleFilesNamespace",
                GenerateMultipleFiles = true
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            var settingsFile = Path.Combine(outputDir, ".refitter");
            File.Exists(settingsFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(settingsFile);
            content.Should().Contain("MultipleFilesNamespace");
            content.Should().Contain("\"generateMultipleFiles\": true");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Creates_File_When_ContractsOutputPath_Is_Set()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputDir = Path.Combine(tempDir, "output");
            var contractsDir = Path.Combine(tempDir, "contracts");

            var settings = new Settings
            {
                OutputPath = outputDir,
                ContractsOutputPath = contractsDir,
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "ContractsNamespace",
                ContractsOutputFolder = contractsDir
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            var settingsFile = Path.Combine(outputDir, ".refitter");
            File.Exists(settingsFile).Should().BeTrue();

            var content = await File.ReadAllTextAsync(settingsFile);
            content.Should().Contain("ContractsNamespace");

            // Verify the deserialized settings contain the correct contracts path
            var deserializedSettings = Serializer.Deserialize<RefitGeneratorSettings>(content);
            deserializedSettings.ContractsOutputFolder.Should().Be(contractsDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Serializes_RefitGeneratorSettings_Correctly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputDir = Path.Combine(tempDir, "output");
            var settings = new Settings
            {
                OutputPath = Path.Combine(outputDir, "MyClient.cs"),
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "TestNamespace",
                GenerateContracts = true,
                GenerateClients = true,
                ReturnIApiResponse = true,
                UseCancellationTokens = true
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            var settingsFile = Path.Combine(outputDir, ".refitter");
            var content = await File.ReadAllTextAsync(settingsFile);

            // Deserialize to verify proper serialization
            var deserializedSettings = Serializer.Deserialize<RefitGeneratorSettings>(content);
            deserializedSettings.Should().NotBeNull();
            deserializedSettings.Namespace.Should().Be("TestNamespace");
            deserializedSettings.GenerateContracts.Should().BeTrue();
            deserializedSettings.GenerateClients.Should().BeTrue();
            deserializedSettings.ReturnIApiResponse.Should().BeTrue();
            deserializedSettings.UseCancellationTokens.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public async Task WriteRefitterSettingsFile_Creates_Directory_If_Not_Exists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var outputDir = Path.Combine(tempDir, "nested", "output", "directory");
            var settings = new Settings
            {
                OutputPath = Path.Combine(outputDir, "MyClient.cs"),
                SimpleOutput = true
            };

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "test.json",
                Namespace = "TestNamespace"
            };

            // Act
            await GenerateCommand.WriteRefitterSettingsFile(settings, refitSettings);

            // Assert
            Directory.Exists(outputDir).Should().BeTrue();
            var settingsFile = Path.Combine(outputDir, ".refitter");
            File.Exists(settingsFile).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
