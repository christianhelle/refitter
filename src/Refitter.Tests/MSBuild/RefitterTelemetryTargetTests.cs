using System.Diagnostics;
using AwesomeAssertions;
using Refitter.SourceGenerator.Telemetry;

namespace Refitter.Tests.MSBuild;

/// <summary>
/// Runs the telemetry target from Refitter.SourceGenerator.targets with the same layout as the NuGet package.
/// Only opted-out builds are run so that the tests never send telemetry.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class RefitterTelemetryTargetTests
{
    private const string TaskName = "RefitterTelemetryTask";
    private string workspace = null!;

    [TestInitialize]
    public void Initialize()
    {
        workspace = Path.Combine(Path.GetTempPath(), "refitter-telemetry-target-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(workspace, "build"));
        Directory.CreateDirectory(Path.Combine(workspace, "tasks", "netstandard2.0"));

        File.Copy(
            Path.Combine(FindRepositoryRoot(), "src", "Refitter.SourceGenerator", "Refitter.SourceGenerator.targets"),
            Path.Combine(workspace, "build", "Refitter.SourceGenerator.targets"));
        File.Copy(
            typeof(RefitterTelemetryTask).Assembly.Location,
            Path.Combine(workspace, "tasks", "netstandard2.0", "Refitter.SourceGenerator.Telemetry.dll"));

        File.WriteAllText(Path.Combine(workspace, "petstore.refitter"), """{ "openApiPath": "./openapi.json" }""");
        File.WriteAllText(
            Path.Combine(workspace, "consumer.proj"),
            """
            <Project>
              <ItemGroup>
                <AdditionalFiles Include="petstore.refitter" />
              </ItemGroup>
              <Target Name="Build" />
              <Import Project="build/Refitter.SourceGenerator.targets" />
            </Project>
            """);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(workspace, true);

    [TestMethod]
    public void Target_Runs_Task_After_Build_And_Task_Honours_Dotnet_Telemetry_Opt_Out()
    {
        var output = RunBuild(string.Empty, telemetryOptOut: "1");

        output.Should().Contain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    [TestMethod]
    public void Target_Is_Skipped_When_RefitterNoLogging_Is_Set()
    {
        var output = RunBuild("-p:RefitterNoLogging=true", telemetryOptOut: "1");

        output.Should().NotContain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    [TestMethod]
    public void Target_Is_Skipped_During_Design_Time_Build()
    {
        var output = RunBuild("-p:DesignTimeBuild=true", telemetryOptOut: "1");

        output.Should().NotContain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    private bool StampFileExists() =>
        File.Exists(Path.Combine(workspace, "refitter.sourcegenerator.telemetry"));

    private string RunBuild(string arguments, string telemetryOptOut)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild consumer.proj -t:Build -v:d -nologo -nodeReuse:false {arguments}",
            WorkingDirectory = workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = telemetryOptOut;

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit(120000).Should().BeTrue();

        var result = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        process.ExitCode.Should().Be(0, result);
        return result;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "Refitter.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Unable to locate the repository root");
    }
}
