using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class OutputPlannerAdapterTests
{
    private static readonly IOutputPlanner planner = new OutputPlannerAdapter();

    [Test]
    public void Plan_SingleFile_DirectCli_Explicit_Output_Is_Used()
    {
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs",
            GenerateMultipleFiles = false,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("GeneratedType", "// code")
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath: null,
            cliOutputPath: Path.Combine("GeneratedCode", "Petstore.cs"));

        planned.Should().ContainSingle();
        planned[0].Path.Should().Be(Path.Combine("GeneratedCode", "Petstore.cs"));
        planned[0].Content.Should().Be("// code");
    }

    [Test]
    public void Plan_SingleFile_DirectCli_Default_Uses_DefaultOutputPath()
    {
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs",
            GenerateMultipleFiles = false,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("GeneratedType", "// code")
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath: null,
            cliOutputPath: OutputPlanner.DefaultOutputPath);

        planned.Should().ContainSingle();
        planned[0].Path.Should().Be(OutputPlanner.DefaultOutputPath);
        planned[0].Content.Should().Be("// code");
    }

    [Test]
    public void Plan_SingleFile_SettingsFile_Roots_Relative_To_Settings_Directory()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            OutputFilename = "Output.cs",
            GenerateMultipleFiles = false,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("GeneratedType", "// code")
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath,
            cliOutputPath: null);

        planned.Should().ContainSingle();
        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Generated", "Output.cs");
        planned[0].Path.Should().Be(expected);
        planned[0].Content.Should().Be("// code");
    }

    [Test]
    public void Plan_MultiFile_DirectCli_Combines_Output_Directory_And_Filename()
    {
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            GenerateMultipleFiles = true,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("RefitInterfaces", "// interfaces"),
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath: null,
            cliOutputPath: Path.Combine("GeneratedCode", "MultipleFiles"));

        planned.Should().ContainSingle();
        planned[0].Path.Should().Be(Path.Combine("GeneratedCode", "MultipleFiles", "RefitInterfaces.cs"));
        planned[0].Content.Should().Be("// interfaces");
    }

    [Test]
    public void Plan_MultiFile_SettingsFile_Roots_Explicit_Cli_Override()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            GenerateMultipleFiles = true,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("RefitInterfaces", "// interfaces"),
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath,
            cliOutputPath: Path.Combine("GeneratedCode", "MultipleFiles"));

        planned.Should().ContainSingle();
        var expected = Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "GeneratedCode", "MultipleFiles", "RefitInterfaces.cs");
        planned[0].Path.Should().Be(expected);
        planned[0].Content.Should().Be("// interfaces");
    }

    [Test]
    public void Plan_MultiFile_Reroutes_Contracts_To_Separate_Folder()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), "Projects", "MyApi", "petstore.refitter");
        var config = new RefitGeneratorSettings
        {
            OutputFolder = "./Generated",
            ContractsOutputFolder = "./Contracts",
            GenerateMultipleFiles = true,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("RefitInterfaces", "// interfaces"),
            new(TypenameConstants.Contracts, "// contracts"),
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath,
            cliOutputPath: null);

        planned.Should().HaveCount(2);
        planned[0].Content.Should().Be("// interfaces");
        planned[1].Content.Should().Be("// contracts");

        var contractsFolder = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(settingsFilePath)!, "./Contracts"));
        planned[1].Path.Should().Be(Path.Combine(contractsFolder, $"{TypenameConstants.Contracts}.cs"));
    }

    [Test]
    public void Plan_Preserves_Content_For_Each_File()
    {
        var config = new RefitGeneratorSettings
        {
            GenerateMultipleFiles = true,
        };

        var output = new GeneratorOutput(new List<GeneratedCode>
        {
            new("Interface1", "// content 1"),
            new("Interface2", "// content 2"),
        });

        var planned = planner.Plan(
            output,
            config,
            settingsFilePath: null,
            cliOutputPath: null);

        planned.Should().HaveCount(2);
        planned[0].Content.Should().Be("// content 1");
        planned[1].Content.Should().Be("// content 2");
    }
}
