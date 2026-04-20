using FluentAssertions;
using Refitter.MSBuild;

namespace Refitter.Tests;

public class RefitterGenerateTaskTests
{
    [Test]
    public void GetValidatedTimeoutSeconds_Should_Fall_Back_To_Default_For_Invalid_Values()
    {
        RefitterGenerateTask.GetValidatedTimeoutSeconds(0).Should().Be(RefitterGenerateTask.DefaultTimeoutSeconds);
        RefitterGenerateTask.GetValidatedTimeoutSeconds(-5).Should().Be(RefitterGenerateTask.DefaultTimeoutSeconds);
    }

    [Test]
    public void GetValidatedTimeoutSeconds_Should_Preserve_Positive_Value()
    {
        RefitterGenerateTask.GetValidatedTimeoutSeconds(42).Should().Be(42);
    }

    [Test]
    public void CreateSummaryMessage_Should_Report_Partial_Failures_Clearly()
    {
        var message = RefitterGenerateTask.CreateSummaryMessage(2, hasErrors: true);

        message.Should().Contain("partially completed");
        message.Should().Contain("generated 2 files");
        message.Should().Contain(".refitter files failed");
    }

    [Test]
    public void CreateSummaryMessage_Should_Report_Success_Without_Failure_Text()
    {
        var message = RefitterGenerateTask.CreateSummaryMessage(3, hasErrors: false);

        message.Should().Be("Generated 3 files.");
    }

    [Test]
    public void SelectRefitterTargetFramework_Should_Fall_Back_When_Higher_Target_Binary_Is_Missing()
    {
        var installedRuntimes = new[]
        {
            "Microsoft.NETCore.App 10.0.0 [C:\\dotnet\\shared\\Microsoft.NETCore.App]",
            "Microsoft.NETCore.App 9.0.0 [C:\\dotnet\\shared\\Microsoft.NETCore.App]",
        };

        var selected = RefitterGenerateTask.SelectRefitterTargetFramework(
            installedRuntimes,
            targetFramework => targetFramework != "net10.0");

        selected.Should().Be("net9.0");
    }

    [Test]
    public void FilterFiles_Should_Not_Use_Substring_Matching_For_Exact_Patterns()
    {
        var files = new[]
        {
            @"C:\repo\pet.refitter",
            @"C:\repo\mypet.refitter",
        };

        var filtered = RefitterGenerateTask.FilterFiles(files, "pet.refitter");

        filtered.Should().Equal(@"C:\repo\pet.refitter");
    }

    [Test]
    public void FilterFiles_Should_Support_Wildcards_Without_OverMatching_Exact_Patterns()
    {
        var files = new[]
        {
            @"C:\repo\pet.refitter",
            @"C:\repo\mypet.refitter",
            @"C:\repo\other.refitter",
        };

        var filtered = RefitterGenerateTask.FilterFiles(files, "*pet.refitter");

        filtered.Should().BeEquivalentTo(
            new[]
            {
                @"C:\repo\pet.refitter",
                @"C:\repo\mypet.refitter",
            });
    }

    [Test]
    public void GetOutputPlan_Should_Resolve_MultiFile_Output_Directories_Without_HardCoding_Interface_Filenames()
    {
        const string json =
            """
            {
              "generateMultipleFiles": true,
              "outputFolder": "./Generated/Clients",
              "contractsOutputFolder": "./Generated/Contracts"
            }
            """;

        var plan = RefitterGenerateTask.GetOutputPlan(@"C:\repo\Api\petstore.refitter", json);

        plan.IsMultiFile.Should().BeTrue();
        plan.CandidateDirectories.Should().BeEquivalentTo(
            new[]
            {
                Path.GetFullPath(@"C:\repo\Api\Generated\Clients"),
                Path.GetFullPath(@"C:\repo\Api\Generated\Contracts"),
            });
    }

    [Test]
    public void CollectGeneratedFiles_Should_Return_Actual_Changed_MultiFile_Outputs()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "RefitterGenerateTaskTests", Guid.NewGuid().ToString("N"));
        var clientsDirectory = Path.Combine(root, "Generated", "Clients");
        var contractsDirectory = Path.Combine(root, "Generated", "Contracts");

        Directory.CreateDirectory(clientsDirectory);
        Directory.CreateDirectory(contractsDirectory);

        try
        {
            var staleFile = Path.Combine(clientsDirectory, "Existing.cs");
            File.WriteAllText(staleFile, "// unchanged");

            var plan = RefitterGenerateTask.RefitterOutputPlan.ForMultipleFiles(new[] { clientsDirectory, contractsDirectory });
            var snapshot = RefitterGenerateTask.CaptureOutputSnapshot(plan);

            var generatedInterface = Path.Combine(clientsDirectory, "PetsApi.cs");
            var generatedContracts = Path.Combine(contractsDirectory, "Contracts.cs");
            var generatedDi = Path.Combine(clientsDirectory, "DependencyInjection.cs");

            File.WriteAllText(generatedInterface, "// new");
            File.WriteAllText(generatedContracts, "// new");
            File.WriteAllText(generatedDi, "// new");

            var generatedFiles = RefitterGenerateTask.CollectGeneratedFiles(plan, snapshot);

            generatedFiles.Should().BeEquivalentTo(
                new[]
                {
                    generatedInterface,
                    generatedContracts,
                    generatedDi,
                });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
