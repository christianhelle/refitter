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
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath: null,
                cliOutputPath: Path.Combine("GeneratedCode", "Petstore.cs"),
                refitSettings)
            .Should().Be(Path.Combine("GeneratedCode", "Petstore.cs"));
    }

    [Test]
    public void SingleFile_DirectCli_Default_Uses_DefaultOutputPath()
    {
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath: null,
                cliOutputPath: OutputPlanner.DefaultOutputPath,
                refitSettings)
            .Should().Be(OutputPlanner.DefaultOutputPath);
    }

    [Test]
    public void SingleFile_SettingsFile_Roots_Relative_To_Settings_Directory()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Generated", "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void MultiFile_DirectCli_Combines_Output_Directory_And_Filename()
    {
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath: null,
                cliOutputPath: Path.Combine("GeneratedCode", "MultipleFiles"),
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine("GeneratedCode", "MultipleFiles", "RefitInterfaces.cs"));
    }

    [Test]
    public void MultiFile_SettingsFile_Roots_Explicit_Cli_Override()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "GeneratedCode", "MultipleFiles", "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath,
                cliOutputPath: Path.Combine("GeneratedCode", "MultipleFiles"),
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
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
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };
        var outputFile = new GeneratedCode(TypenameConstants.Contracts, "// code");

        var expectedFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Contracts"));
        OutputPlanner.GetContractsOutputPath(settingsFilePath, refitSettings, outputFile)
            .Should().Be(Path.Combine(expectedFolder, ContractsFileName));
    }

    [Test]
    public void PlanMultipleFiles_Reroutes_Only_The_Contracts_File()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", ContractsOutputFolder = "./Contracts" };

        var interfaces = new GeneratedCode("RefitInterfaces", "// interfaces");
        var contracts = new GeneratedCode(TypenameConstants.Contracts, "// contracts");
        var generatorOutput = new GeneratorOutput(new List<GeneratedCode> { interfaces, contracts });

        var planned = OutputPlanner.PlanMultipleFiles(settingsFilePath, cliOutputPath: null, refitSettings, generatorOutput);

        planned.Should().HaveCount(2);
        planned[0].Content.Should().Be("// interfaces");
        planned[1].Content.Should().Be("// contracts");

        var contractsFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Contracts"));
        planned[0].Path.Should().Be(OutputPlanner.GetMultiFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings, interfaces));
        planned[1].Path.Should().Be(Path.Combine(contractsFolder, ContractsFileName));
    }

    [Test]
    public void PlanSingleFile_Preserves_Content()
    {
        var refitSettings = new RefitGeneratorSettings();

        var planned = OutputPlanner.PlanSingleFile(
            settingsFilePath: null,
            cliOutputPath: Path.Combine("Generated", "Api.cs"),
            refitSettings,
            "// generated");

        planned.Content.Should().Be("// generated");
        planned.Path.Should().Be(Path.Combine("Generated", "Api.cs"));
    }

    [Test]
    public void SingleFile_SettingsFile_CliOverride_Output_Uses_CliPath()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated", OutputFilename = "Output.cs" };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "CustomCli",
            "Override.cs");
        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath,
                cliOutputPath: Path.Combine("CustomCli", "Override.cs"),
                refitSettings)
            .Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_NoOutputOverride_Uses_SettingsFile_OutputFolder_And_Filename()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./CustomFolder", OutputFilename = "CustomOutput.cs" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./CustomFolder", "CustomOutput.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_DefaultOutputFolder_Uses_DefaultFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFilename = "ApiClient.cs" };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            RefitGeneratorSettings.DefaultOutputFolder,
            "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_Rooted_Path_Does_Not_Combine_With_Root()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = Path.GetTempPath() };

        var expected = Path.Combine(Path.GetTempPath(), "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_DirectCli_Defaults_To_DefaultOutputPath()
    {
        var refitSettings = new RefitGeneratorSettings();

        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath: null,
                cliOutputPath: OutputPlanner.DefaultOutputPath,
                refitSettings)
            .Should().Be(OutputPlanner.DefaultOutputPath);
    }

    [Test]
    public void SingleFile_DirectCli_Empty_SettingsFilePath_Defaults()
    {
        var refitSettings = new RefitGeneratorSettings();

        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath: string.Empty,
                cliOutputPath: OutputPlanner.DefaultOutputPath,
                refitSettings)
            .Should().Be(OutputPlanner.DefaultOutputPath);
    }

    [Test]
    public void MultiFile_DirectCli_Default_Output_Uses_Current_Directory()
    {
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./Generated" };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath: null,
                cliOutputPath: OutputPlanner.DefaultOutputPath,
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine(".", "RefitInterfaces.cs"));
    }

    [Test]
    public void MultiFile_SettingsFile_Uses_RefitGeneratorSettings_OutputFolder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = "./ApiClient" };

        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./ApiClient", "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(expected);
    }

    [Test]
    public void MultiFile_SettingsFile_Rooted_OutputFolder_Does_Not_Combine_With_Root()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings { OutputFolder = Path.GetTempPath() };

        var expected = Path.Combine(Path.GetTempPath(), "RefitInterfaces.cs");
        OutputPlanner.GetMultiFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings, new GeneratedCode("RefitInterfaces", "// code"))
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
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };
        var outputFile = new GeneratedCode(TypenameConstants.Contracts, "// code");

        var contractsFolder = Path.GetFullPath("./Contracts");
        OutputPlanner.GetContractsOutputPath(
                settingsFilePath: null,
                refitSettings,
                outputFile)
            .Should().Be(Path.Combine(contractsFolder, TypenameConstants.Contracts + ".cs"));
    }

    [Test]
    public void SingleFile_SettingsFile_NullOutputFilename_Uses_Default()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomFolder",
            OutputFilename = null!
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "./CustomFolder",
            "Output.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_NullOutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = null!,
            OutputFilename = "ApiClient.cs"
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings).Should().Be(expected);
    }

    [Test]
    public void MultiFile_SettingsFile_NullOutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = null!,
        };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath,
                cliOutputPath: null,
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine(
                Path.GetDirectoryName(settingsFilePath)!,
                "RefitInterfaces.cs"));
    }

    [Test]
    public void SingleFile_BareFilename_SettingsFilePath_Handles_Empty_Root()
    {
        var settingsFilePath = "petstore.refitter";
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "ApiClient.cs"
        };

        var expected = Path.Combine("./Generated", "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings)
            .Should().Be(expected);
    }

    [Test]
    public void SingleFile_BareFilename_SettingsFilePath_With_CliOverride_Roots_To_CliPath()
    {
        var settingsFilePath = "petstore.refitter";
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "ApiClient.cs"
        };

        OutputPlanner.GetSingleFileOutputPath(
                settingsFilePath,
                cliOutputPath: Path.Combine("CustomCli", "Override.cs"),
                refitSettings)
            .Should().Be(Path.Combine("CustomCli", "Override.cs"));
    }

    [Test]
    public void MultiFile_BareFilename_SettingsFilePath_Handles_Empty_Root()
    {
        var settingsFilePath = "petstore.refitter";
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./ApiClient"
        };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath,
                cliOutputPath: null,
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine("./ApiClient", "RefitInterfaces.cs"));
    }

    [Test]
    public void PlanMultipleFiles_DirectCli_No_SettingsFilePath()
    {
        var refitSettings = new RefitGeneratorSettings();
        var generatorOutput = new GeneratorOutput(new List<GeneratedCode>
        {
            new("RefitInterfaces", "// interfaces"),
            new(TypenameConstants.Contracts, "// contracts")
        });

        var planned = OutputPlanner.PlanMultipleFiles(
            settingsFilePath: null,
            cliOutputPath: OutputPlanner.DefaultOutputPath,
            refitSettings,
            generatorOutput);

        planned.Should().HaveCount(2);
        planned[0].Path.Should().Be(Path.Combine(".", "RefitInterfaces.cs"));
        planned[1].Path.Should().Be(Path.Combine(".", $"{TypenameConstants.Contracts}.cs"));
    }

    [Test]
    public void GetContractsOutputPath_With_Empty_String_SettingsFilePath()
    {
        var refitSettings = new RefitGeneratorSettings { ContractsOutputFolder = "./Contracts" };
        var outputFile = new GeneratedCode(TypenameConstants.Contracts, "// code");

        var contractsFolder = Path.GetFullPath("./Contracts");
        OutputPlanner.GetContractsOutputPath(
                settingsFilePath: string.Empty,
                refitSettings,
                outputFile)
            .Should().Be(Path.Combine(contractsFolder, TypenameConstants.Contracts + ".cs"));
    }

    [Test]
    public void SingleFile_SettingsFile_WhitespaceOutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = " ",
            OutputFilename = "ApiClient.cs"
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings)
            .Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_EmptyOutputFilename_Uses_Empty_String()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./CustomFolder",
            OutputFilename = string.Empty
        };

        var expected = Path.Combine(
            Path.GetDirectoryName(settingsFilePath)!,
            "./CustomFolder",
            string.Empty);
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings)
            .Should().Be(expected);
    }

    [Test]
    public void SingleFile_SettingsFile_RootDir_As_SettingsFilePath()
    {
        var rootDir = Path.GetPathRoot(Path.GetTempPath())!;
        var settingsFilePath = Path.Combine(rootDir, "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "ApiClient.cs"
        };

        var expected = Path.Combine(rootDir, "./Generated", "ApiClient.cs");
        OutputPlanner.GetSingleFileOutputPath(settingsFilePath, cliOutputPath: null, refitSettings)
            .Should().Be(expected);
    }

    [Test]
    public void MultiFile_SettingsFile_Empty_OutputFolder_Uses_Filename_Directly()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = string.Empty
        };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath,
                cliOutputPath: null,
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine(
                Path.GetDirectoryName(settingsFilePath)!,
                "RefitInterfaces.cs"));
    }

    [Test]
    public void MultiFile_BareFilename_SettingsFile_With_CliOverride_Roots_To_CliPath()
    {
        var settingsFilePath = "petstore.refitter";
        var refitSettings = new RefitGeneratorSettings
        {
            OutputFolder = "./ApiClient"
        };

        OutputPlanner.GetMultiFileOutputPath(
                settingsFilePath,
                cliOutputPath: Path.Combine("CustomCli", "MultipleFiles"),
                refitSettings,
                new GeneratedCode("RefitInterfaces", "// code"))
            .Should().Be(Path.Combine("CustomCli", "MultipleFiles", "RefitInterfaces.cs"));
    }
}
