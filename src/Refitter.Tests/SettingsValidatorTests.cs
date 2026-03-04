using System.Text.Json;
using FluentAssertions;
using Refitter.Core;
using Spectre.Console;
using TUnit.Core;

namespace Refitter.Tests;

public class SettingsValidatorTests
{
    [Test]
    public void Validate_Should_Fail_When_Both_OpenApiPath_And_SettingsFilePath_Are_Empty()
    {
        var settings = new Settings
        {
            OpenApiPath = null,
            SettingsFilePath = null
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("either specify an input URL/file directly");
    }

    [Test]
    public void Validate_Should_Fail_When_Both_OpenApiPath_And_SettingsFilePath_Are_Present()
    {
        var settings = new Settings
        {
            OpenApiPath = "openapi.json",
            SettingsFilePath = "settings.refitter"
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("either specify an input URL/file directly");
    }

    [Test]
    public void Validate_Should_Succeed_With_Valid_URL()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://petstore3.swagger.io/api/v3/openapi.yaml"
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Succeed_With_Valid_Http_URL()
    {
        var settings = new Settings
        {
            OpenApiPath = "http://example.com/openapi.json"
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Fail_When_File_Does_Not_Exist()
    {
        var settings = new Settings
        {
            OpenApiPath = "nonexistent-file.json"
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("File not found");
    }

    [Test]
    public void Validate_Should_Succeed_When_File_Exists()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{}");
            var settings = new Settings
            {
                OpenApiPath = tempFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public void Validate_Should_Fail_When_OperationNameTemplate_Missing_Placeholder()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "MyTemplate",
            MultipleInterfaces = MultipleInterfaces.Unset
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("{operationName}");
    }

    [Test]
    public void Validate_Should_Succeed_When_OperationNameTemplate_Has_Placeholder()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "{operationName}Async"
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Succeed_When_OperationNameTemplate_Missing_Placeholder_With_ByEndpoint()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "Execute",
            MultipleInterfaces = MultipleInterfaces.ByEndpoint
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Succeed_With_Valid_Settings_File()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "https://petstore3.swagger.io/api/v3/openapi.yaml"
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
            settings.OpenApiPath.Should().Be("https://petstore3.swagger.io/api/v3/openapi.yaml");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Validate_Should_Fail_When_Settings_File_Has_Empty_OpenApiPath()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = ""
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("'openApiPath' or 'openApiPaths' in settings file is required");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Validate_Should_Fail_When_Both_Output_Paths_Are_Specified()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "https://example.com/openapi.json",
                OutputFolder = "./output",
                OutputFilename = "Generated.cs"
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile,
                OutputPath = "./custom/Output.cs"
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("either specify an output path directly from --output");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Validate_Should_Succeed_When_OutputPath_Is_Default_And_Settings_File_Has_Output()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "https://example.com/openapi.json",
                OutputFolder = "./output",
                OutputFilename = "Generated.cs"
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile,
                OutputPath = Settings.DefaultOutputPath
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Validate_Should_Succeed_When_OperationNameTemplate_Is_Null()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = null
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Succeed_When_OperationNameTemplate_Is_Empty()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = ""
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Succeed_With_Valid_OpenApiPaths_In_Settings_File()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = ["https://petstore3.swagger.io/api/v3/openapi.yaml"]
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
            settings.OpenApiPath.Should().Be("https://petstore3.swagger.io/api/v3/openapi.yaml");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Validate_Should_Succeed_With_Multiple_OpenApiPaths_In_Settings_File()
    {
        var tempSettingsFile = Path.GetTempFileName();
        var tempApiFile1 = Path.GetTempFileName();
        var tempApiFile2 = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempApiFile1, "{}");
            File.WriteAllText(tempApiFile2, "{}");

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = [tempApiFile1, tempApiFile2]
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
            settings.OpenApiPath.Should().Be(tempApiFile1);
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
            if (File.Exists(tempApiFile1))
                File.Delete(tempApiFile1);
            if (File.Exists(tempApiFile2))
                File.Delete(tempApiFile2);
        }
    }

    [Test]
    public void Validate_Should_Fail_When_Settings_File_Has_Empty_OpenApiPaths()
    {
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = []
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("'openApiPath' or 'openApiPaths' in settings file is required");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }
}
