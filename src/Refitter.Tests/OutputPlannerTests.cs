using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class OutputPlannerTests
{
    private static readonly string ContractsFileName = $"{TypenameConstants.Contracts}.cs";

    [Test]
    public void SingleFile_DirectCli_Explicit_Output_Is_Used()
    {
        var settings = new Settings { OutputPath = Path.Combine("GeneratedCode", "Petstore.cs") };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings)
            .Should().Be(Path.Combine("GeneratedCode", "Petstore.cs"));
    }

    [Test]
    public void SingleFile_DirectCli_Default_Uses_DefaultOutputPath()
    {
        var settings = new Settings { OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings)
            .Should().Be(Settings.DefaultOutputPath);
    }

    [Test]
    public void SingleFile_SettingsFile_Roots_Relative_To_Settings_Directory()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Generated", "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void MultiFile_DirectCli_Combines_Output_Directory_And_Filename()
    {
        var settings = new Settings { OutputPath = Path.Combine("GeneratedCode", "MultipleFiles") };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine("GeneratedCode", "MultipleFiles", "RefitInterfaces.cs"));
    }

    [Test]
    public void MultiFile_SettingsFile_Roots_Explicit_Cli_Override()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Path.Combine("GeneratedCode", "MultipleFiles")
        };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "GeneratedCode", "MultipleFiles", "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(expected);
    }

    [Test]
    public void ShouldReroute_Is_True_For_Contracts_File_With_Custom_Folder()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };

        OutputPlanner.ShouldRerouteToContractsFolder(refitSettings, new GeneratedCode(TypenameConstants.Contracts, "// code"))
            .Should().BeTrue();
    }

    [Test]
    public void ShouldReroute_Is_False_For_Default_Contracts_Folder()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = RefitGeneratorSettings.DefaultOutputFolder };

        OutputPlanner.ShouldRerouteToContractsFolder(refitSettings, new GeneratedCode(TypenameConstants.Contracts, "// code"))
            .Should().BeFalse();
    }

    [Test]
    public void ShouldReroute_Is_False_For_Non_Contracts_File()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };

        OutputPlanner.ShouldRerouteToContractsFolder(refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().BeFalse();
    }

    [Test]
    public void ShouldReroute_Is_False_When_Contracts_Folder_Empty()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = string.Empty };

        OutputPlanner.ShouldRerouteToContractsFolder(refitSettings, new GeneratedCode(TypenameConstants.Contracts, "// code"))
            .Should().BeFalse();
    }

    [Test]
    public void GetContractsOutputPath_Roots_Absolute_Under_Settings_Directory()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath };
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };
        var outputFile = new GeneratedCode(TypenameConstants.Contracts, "// code");

        var expectedFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Contracts"));
        OutputPlanner.GetContractsOutputPath(settings, refitSettings, outputFile)
            .Should().Be(Path.Combine(expectedFolder, ContractsFileName));
    }

    [Test]
    public void PlanMultipleFiles_Reroutes_Only_The_Contracts_File()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", ContractsOutputFolder = "./Contracts" };

        var interfaces = new GeneratedCode("RefitInterfaces", "// interfaces");
        var contracts = new GeneratedCode(TypenameConstants.Contracts, "// contracts");
        var generatorOutput = new GeneratorOutput(new List<GeneratedCode> { interfaces, contracts });

        var planned = OutputPlanner.PlanMultipleFiles(settings, refitSettings, generatorOutput);

        planned.Should().HaveCount(2);
        planned[0].Content.Should().Be("// interfaces");
        planned[1].Content.Should().Be("// contracts");

        var contractsFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Contracts"));
        planned[0].Path.Should().Be(OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, interfaces));
        planned[1].Path.Should().Be(Path.Combine(contractsFolder, ContractsFileName));
    }

    [Test]
    public void PlanSingleFile_Preserves_Content()
    {
        var settings = new Settings { OutputPath = Path.Combine("Generated", "Api.cs") };
        var refitSettings = new RefitGeneratorSettings();

        var planned = OutputPlanner.PlanSingleFile(settings, refitSettings, "// generated");

        planned.Content.Should().Be("// generated");
        planned.Path.Should().Be(Path.Combine("Generated", "Api.cs"));
    }

    [Test]
    public void SingleFile_SettingsFile_CliOverride_Output_Uses_CliPath()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Path.Combine("CustomCli", "Override.cs")
        };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "CustomCli",
            "Override.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_NoOutputOverride_Uses_SettingsFile_OutputFolder_And_Filename()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./CustomFolder", OutputFilename = "CustomOutput.cs" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./CustomFolder", "CustomOutput.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_DefaultOutputFolder_Uses_DefaultFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFilename = "ApiClient.cs" };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            RefitGeneratorSettings.DefaultOutputFolder,
            "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_Rooted_Path_Does_Not_Combine_With_Root()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = Path.GetTempPath() };

        var expected = Path.Combine(Path.GetTempPath(), "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_DirectCli_Defaults_To_DefaultOutputPath()
    {
        var settings = new Settings { SettingsFilePath = null, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings();

        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(Settings.DefaultOutputPath);
    }

    [Test]
    public void SingleFile_DirectCli_Empty_SettingsFilePath_Defaults()
    {
        var settings = new Settings { SettingsFilePath = string.Empty, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings();

        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(Settings.DefaultOutputPath);
    }

    [Test]
    public void MultiFile_DirectCli_Default_Output_Uses_Current_Directory()
    {
        var settings = new Settings { OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine(".", "RefitInterfaces.cs"));
    }

    [Test]
    public void MultiFile_SettingsFile_Uses_RefitGeneratorSettings_OutputFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./ApiClient" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./ApiClient", "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(expected);
    }

    [Test]
    public void MultiFile_SettingsFile_Rooted_OutputFolder_Does_Not_Combine_With_Root()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings { OutputFolder = Path.GetTempPath() };

        var expected = Path.Combine(Path.GetTempPath(), "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(expected);
    }

    [Test]
    public void ShouldReroute_Is_False_When_Contracts_Folder_Equals_DefaultOutputFolder()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Generated" };

        OutputPlanner.ShouldRerouteToContractsFolder(refitSettings, new GeneratedCode(TypenameConstants.Contracts, "// code"))
            .Should().BeFalse();
    }

    [Test]
    public void GetContractsOutputPath_Without_SettingsFilePath_Uses_ContractsFolder_Directly()
    {
        var settings = new Settings();
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };
        var outputFile = new GeneratedCode(TypenameConstants.Contracts, "// code");

        var contractsFolder = Path.GetFullPath("./Contracts");
        OutputPlanner.GetContractsOutputPath(settings, refitSettings, outputFile)
            .Should().Be(Path.Combine(contractsFolder, TypenameConstants.Contracts + ".cs"));
    }

    [Test]
    public void SingleFile_SettingsFile_NullOutputFilename_Uses_Default()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomFolder",
            OutputFilename = null!
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "./CustomFolder",
            "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_NullOutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings { SettingsFilePath = settingsFilePath, OutputPath = Settings.DefaultOutputPath };
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = null!,
            OutputFilename = "ApiClient.cs"
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settings, refitSettings).Should().Be(expected);
    }

    [Test]
    public void MultiFile_SettingsFile_NullOutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var settings = new Settings
        {
            SettingsFilePath = settingsFilePath,
            OutputPath = Settings.DefaultOutputPath
        };
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = null!,
        };

        OutputPlanner.GetMultiFileOutputPath(settings, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine(
                Path.GetDirectoryName(settingsFilePath)!,
                "RefitInterfaces.cs"));
    }
}
