using System.Text.Json;
using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.RegressionTests;

/// <summary>
/// Regression tests for #1057 settings and CLI workstream issues.
/// Tests issues: #1021, #1030, #1031, #1044, #1045, #1046, #1048, #1050, #1054
/// </summary>
public class SettingsCliRegressionTests
{
    [Test]
    public void Issue1054_CreateAsync_Should_Throw_ArgumentNullException_For_Null_Paths()
    {
        // #1054: Correct exception type for null CreateAsync(IEnumerable)
        Func<Task> act = async () => await OpenApiDocumentFactory.CreateAsync((IEnumerable<string>)null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task RefitGenerator_CreateAsync_Should_Throw_When_Both_OpenApiPath_And_OpenApiPaths_Are_Missing()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = null,
            OpenApiPaths = null
        };

        Func<Task> act = async () => await RefitGenerator.CreateAsync(settings);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*OpenApiPath*OpenApiPaths*");
    }

    [Test]
    public async Task RefitGenerator_CreateAsync_Should_Throw_When_OpenApiPath_Is_Whitespace_And_OpenApiPaths_Is_Empty()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "   ",
            OpenApiPaths = Array.Empty<string>()
        };

        Func<Task> act = async () => await RefitGenerator.CreateAsync(settings);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*OpenApiPath*OpenApiPaths*");
    }

    [Test]
    public void Issue1046_OpenApiPaths_Should_Not_Serialize_When_Null()
    {
        // #1046: Default OpenApiPaths round-trip behavior
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "test.json",
            OpenApiPaths = null
        };

        var json = Serializer.Serialize(settings);
        json.Should().NotContain("openApiPaths");
    }

    [Test]
    public void Issue1046_OpenApiPaths_Should_Not_Serialize_When_Default()
    {
        // #1046: Default OpenApiPaths round-trip behavior
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "test.json"
            // OpenApiPaths not set (null by default)
        };

        var json = Serializer.Serialize(settings);
        json.Should().NotContain("openApiPaths");
    }

    [Test]
    public void Issue1045_OpenApiPath_And_OpenApiPaths_Are_Independent()
    {
        // #1045: OpenApiPath and OpenApiPaths are separate properties
        // Note: The computed property approach (OpenApiPath returning first from OpenApiPaths)
        // was causing serialization issues, so they remain independent
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = new[] { "path1.json", "path2.json" }
        };

        settings.OpenApiPath.Should().BeNull();
    }

    [Test]
    public void Issue1045_OpenApiPath_Should_Be_Null_When_OpenApiPaths_Empty()
    {
        // #1045: OpenApiPath should be null when OpenApiPaths is empty
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = Array.Empty<string>()
        };

        settings.OpenApiPath.Should().BeNull();
    }

    [Test]
    public void Issue1045_OpenApiPath_Should_Prefer_Explicit_Value_Over_OpenApiPaths()
    {
        // #1045: Explicit OpenApiPath takes precedence
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "explicit.json",
            OpenApiPaths = new[] { "path1.json", "path2.json" }
        };

        settings.OpenApiPath.Should().Be("explicit.json");
    }

    [Test]
    public void Issue1044_Validate_Should_Fail_When_Both_OpenApiPath_And_OpenApiPaths_Are_Set()
    {
        // #1044: OpenApiPath + OpenApiPaths precedence validation
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPath = "single.json",
                OpenApiPaths = new[] { "multi1.json", "multi2.json" }
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("Cannot specify both 'openApiPath' and 'openApiPaths'");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Issue1030_Validate_Should_Check_All_OpenApiPaths_Entries()
    {
        // #1030: Validate all openApiPaths
        var tempSettingsFile = Path.GetTempFileName();
        var tempApiFile1 = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempApiFile1, "{}");

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { tempApiFile1, "nonexistent-file-xyz.json" }
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("openApiPaths[1]");
            result.Message.Should().Contain("nonexistent-file-xyz.json");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
            if (File.Exists(tempApiFile1))
                File.Delete(tempApiFile1);
        }
    }

    [Test]
    public void Issue1030_Validate_Should_Succeed_When_All_OpenApiPaths_Exist()
    {
        // #1030: Validate all openApiPaths
        var tempSettingsFile = Path.GetTempFileName();
        var tempApiFile1 = Path.GetTempFileName();
        var tempApiFile2 = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempApiFile1, "{}");
            File.WriteAllText(tempApiFile2, "{}");

            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { tempApiFile1, tempApiFile2 }
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeTrue();
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
    public void Issue1030_Validate_Should_Allow_URLs_In_OpenApiPaths()
    {
        // #1030: URLs don't need file existence check
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            var refitSettings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[]
                {
                    "https://petstore3.swagger.io/api/v3/openapi.json",
                    "http://example.com/api.yaml"
                }
            };
            File.WriteAllText(tempSettingsFile, JsonSerializer.Serialize(refitSettings));

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
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
    public void Issue1050_Validate_Should_Provide_Clear_Error_For_Invalid_Enum()
    {
        // #1050: Improve enum-deserialization error clarity
        var tempSettingsFile = Path.GetTempFileName();
        try
        {
            // Create settings file with invalid enum value
            var invalidJson = @"{
                ""openApiPath"": ""test.json"",
                ""propertyNamingPolicy"": ""InvalidValue""
            }";
            File.WriteAllText(tempSettingsFile, invalidJson);

            var settings = new Settings
            {
                SettingsFilePath = tempSettingsFile
            };

            var result = SettingsValidator.Validate(settings);

            result.Successful.Should().BeFalse();
            result.Message.Should().Contain("Invalid value in settings file");
            result.Message.Should().Contain("propertyNamingPolicy");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }

    [Test]
    public void Issue1021_CLI_Output_Should_Override_Settings_File_Output()
    {
        // #1021: CLI --output no longer overrides when settings file used
        var settings = new Settings
        {
            SettingsFilePath = "test.refitter",
            OutputPath = "./CustomOutput/MyApi.cs"
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            new[] { typeof(Settings), typeof(RefitGeneratorSettings) });

        method.Should().NotBeNull();
        var result = method!.Invoke(null, new object[] { settings, refitSettings }) as string;

        // CLI output should override settings file
        // Normalize path separators for cross-platform compatibility
        result.Should().NotBeNull();
        Path.GetFullPath(result!).Should().EndWith(Path.Combine("CustomOutput", "MyApi.cs"));
    }

    [Test]
    public void Issue1021_Settings_File_Output_Should_Be_Used_When_No_CLI_Override()
    {
        // #1021: Settings file should be used when no explicit CLI output
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Settings.DefaultOutputPath // Default, not an override
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomFolder",
            OutputFilename = "CustomApi.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            new[] { typeof(Settings), typeof(RefitGeneratorSettings) });

        method.Should().NotBeNull();
        var result = method!.Invoke(null, new object[] { settings, refitSettings }) as string;

        // Should use settings file output
        result.Should().Contain("CustomFolder");
        result.Should().EndWith("CustomApi.cs");
    }

    [Test]
    public void Issue1048_Validate_Should_Cache_Settings_To_Avoid_Double_Read()
    {
        // #1048: CLI reads .refitter twice
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

            var result = SettingsValidator.Validate(settings, out var cachedSettings);

            result.Successful.Should().BeTrue();
            cachedSettings.Should().NotBeNull();
            cachedSettings!.OpenApiPath.Should().Be("https://petstore3.swagger.io/api/v3/openapi.yaml");
        }
        finally
        {
            if (File.Exists(tempSettingsFile))
                File.Delete(tempSettingsFile);
        }
    }
}
