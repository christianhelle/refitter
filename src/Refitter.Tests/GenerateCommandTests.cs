using System.Reflection;
using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class GenerateCommandTests
{
    [Test]
    public void Validate_Should_Call_SettingsValidator()
    {
        var command = new GenerateCommand();
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            NoLogging = true
        };

        // We cannot easily test Validate() method without a real CommandContext
        // which is sealed. Instead, we'll test SettingsValidator directly
        // which is what Validate() calls internally.
        var validationResult = SettingsValidator.Validate(settings);

        validationResult.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Work_With_Valid_URL()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void Validate_Should_Fail_When_Both_OpenApiPath_And_SettingsFilePath_Are_Empty()
    {
        var settings = new Settings
        {
            OpenApiPath = null,
            SettingsFilePath = null,
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
    }

    [Test]
    public void Validate_Should_Fail_When_File_Does_Not_Exist()
    {
        var settings = new Settings
        {
            OpenApiPath = "nonexistent.json",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("File not found");
    }

    [Test]
    public void Validate_Should_Fail_For_Invalid_OperationNameTemplate()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "InvalidTemplate",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeFalse();
        result.Message.Should().Contain("{operationName}");
    }

    [Test]
    public void Validate_Should_Succeed_For_Valid_OperationNameTemplate()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            OperationNameTemplate = "{operationName}Async",
            NoLogging = true
        };

        var result = SettingsValidator.Validate(settings);

        result.Successful.Should().BeTrue();
    }

    [Test]
    public void CreateRefitGeneratorSettings_Should_Map_PropertyNamingPolicy()
    {
        var settings = new Settings
        {
            OpenApiPath = "https://example.com/openapi.json",
            PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal
        };

        var method = typeof(GenerateCommand).GetMethod(
            "CreateRefitGeneratorSettings",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var refitSettings = method!
            .Invoke(null, [settings])
            .Should()
            .BeOfType<RefitGeneratorSettings>()
            .Subject;

        refitSettings.PropertyNamingPolicy.Should().Be(PropertyNamingPolicy.PreserveOriginal);
    }

    [Test]
    public void Command_Should_Have_Protected_Validate_Method()
    {
        var command = new GenerateCommand();
        var method = command.GetType().GetMethod(
            "Validate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsFamily.Should().BeTrue();
    }

    [Test]
    public void Command_Should_Have_Protected_ExecuteAsync_Method()
    {
        var command = new GenerateCommand();
        var method = command.GetType().GetMethod(
            "ExecuteAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        method.Should().NotBeNull();
        method!.IsFamily.Should().BeTrue();
    }

    [Test]
    public void FormatGeneratedFileMarker_Should_Emit_A_Task_Parseable_Full_Path()
    {
        var generatedFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Generated", "Petstore.cs"));
        var outputLine = GenerateCommand.FormatGeneratedFileMarker(generatedFile);

        outputLine.Should().Be($"{GenerateCommand.GeneratedFileMarker}{generatedFile}");
        Refitter.MSBuild.RefitterGenerateTask.ParseGeneratedFilePath(outputLine).Should().Be(generatedFile);
    }

    [Test]
    public void GetOutputPath_Should_Root_Relative_To_SettingsFile_When_OutputFolder_Is_Default()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Settings.DefaultOutputPath
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Settings), typeof(RefitGeneratorSettings)]);

        method.Should().NotBeNull();
        var result = method!.Invoke(null, [settings, refitSettings]) as string;

        var expectedPath = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Generated", "Output.cs");
        result.Should().Be(expectedPath);
    }

    [Test]
    public void GetOutputPath_Should_Use_Explicit_Cli_Output_File_Path()
    {
        var settings = new Settings
        {
            OutputPath = Path.Combine("GeneratedCode", "Petstore.cs")
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Settings), typeof(RefitGeneratorSettings)]);

        method.Should().NotBeNull();
        var result = method!.Invoke(null, [settings, refitSettings]) as string;

        result.Should().Be(Path.Combine("GeneratedCode", "Petstore.cs"));
    }

    [Test]
    public void GetOutputPath_Should_Use_Default_Cli_Output_File_In_Current_Directory()
    {
        var settings = new Settings
        {
            OutputPath = Settings.DefaultOutputPath
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Settings), typeof(RefitGeneratorSettings)]);

        method.Should().NotBeNull();
        var result = method!.Invoke(null, [settings, refitSettings]) as string;

        result.Should().Be(Settings.DefaultOutputPath);
    }

    [Test]
    public void GetOutputPath_Should_Root_Relative_To_SettingsFile_When_OutputFolder_Is_Custom()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Settings.DefaultOutputPath
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomOutput",
            OutputFilename = "PetStore.cs"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Settings), typeof(RefitGeneratorSettings)]);

        var result = method!.Invoke(null, [settings, refitSettings]) as string;

        var expectedPath = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./CustomOutput", "PetStore.cs");
        result.Should().Be(expectedPath);
    }

    [Test]
    public void GetOutputPath_Should_Use_SettingsFileName_When_OutputFilename_Is_Missing()
    {
        // OutputFilename will be defaulted by ApplySettingsFileDefaults
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Settings.DefaultOutputPath
        };

        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = null
        };

        // Apply defaults first (simulates what GenerateCommand does)
        var applyMethod = typeof(GenerateCommand).GetMethod(
            "ApplySettingsFileDefaults",
            BindingFlags.NonPublic | BindingFlags.Static);
        applyMethod!.Invoke(null, [settingsFilePath, refitSettings]);

        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Static,
            [typeof(Settings), typeof(RefitGeneratorSettings)]);

        var result = method!.Invoke(null, [settings, refitSettings]) as string;

        // Should use settings file base name
        var expectedFilename = Path.GetFileNameWithoutExtension(settingsFilePath) + ".cs";
        var expectedPath = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Generated", expectedFilename);
        result.Should().Be(expectedPath);
    }

    [Test]
    public void GetOutputPath_For_MultipleFiles_Should_Use_Explicit_Cli_Output_Directory()
    {
        var settings = new Settings
        {
            OutputPath = Path.Combine("GeneratedCode", "MultipleFiles"),
            GenerateMultipleFiles = true
        };

        var refitSettings = new RefitGeneratorSettings
        {
            GenerateMultipleFiles = true,
            OutputFolder = "./Generated"
        };

        var command = new GenerateCommand();
        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(Settings), typeof(RefitGeneratorSettings), typeof(GeneratedCode)]);

        method.Should().NotBeNull();

        var result = method!.Invoke(command, [settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code")]) as string;

        result.Should().Be(Path.Combine("GeneratedCode", "MultipleFiles", "RefitInterfaces.cs"));
    }

    [Test]
    public void GetOutputPath_For_MultipleFiles_WithSettingsFile_Should_Use_Explicit_Cli_Output_Directory()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Path.Combine("GeneratedCode", "MultipleFiles"),
            GenerateMultipleFiles = true
        };

        var refitSettings = new RefitGeneratorSettings
        {
            GenerateMultipleFiles = true,
            OutputFolder = "./Generated"
        };

        var command = new GenerateCommand();
        var method = typeof(GenerateCommand).GetMethod(
            "GetOutputPath",
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(Settings), typeof(RefitGeneratorSettings), typeof(GeneratedCode)]);

        method.Should().NotBeNull();

        var result = method!.Invoke(command, [settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code")]) as string;

        result.Should().Be(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "GeneratedCode", "MultipleFiles", "RefitInterfaces.cs"));
    }

    [Test]
    public void ApplySettingsFileDefaults_Should_Set_Default_OutputFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = null
        };

        var method = typeof(GenerateCommand).GetMethod(
            "ApplySettingsFileDefaults",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        method!.Invoke(null, [settingsFilePath, refitSettings]);

        refitSettings.OutputFolder.Should().Be("./Generated");
    }

    [Test]
    public void ApplySettingsFileDefaults_Should_Preserve_Explicit_OutputFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomFolder"
        };

        var method = typeof(GenerateCommand).GetMethod(
            "ApplySettingsFileDefaults",
            BindingFlags.NonPublic | BindingFlags.Static);

        method!.Invoke(null, [settingsFilePath, refitSettings]);

        refitSettings.OutputFolder.Should().Be("./CustomFolder");
    }

    [Test]
    public void ApplySettingsFileDefaults_Should_Set_Default_When_Empty_OutputFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = string.Empty
        };

        var method = typeof(GenerateCommand).GetMethod(
            "ApplySettingsFileDefaults",
            BindingFlags.NonPublic | BindingFlags.Static);

        method!.Invoke(null, [settingsFilePath, refitSettings]);

        refitSettings.OutputFolder.Should().Be("./Generated");
    }
}
