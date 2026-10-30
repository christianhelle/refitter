using AwesomeAssertions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Refitter.SourceGenerator.Telemetry;

namespace Refitter.Tests.MSBuild;

[TestClass]
public class RefitterTelemetryTaskTests
{
    private string workspace = null!;
    private List<IReadOnlyList<TelemetryEvent>> sentPayloads = null!;
    private Action<IReadOnlyList<TelemetryEvent>, int>? sender;

    [TestInitialize]
    public void Initialize()
    {
        workspace = Path.Combine(Path.GetTempPath(), "refitter-telemetry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        sentPayloads = [];
        sender = null;
    }

    [TestCleanup]
    public void Cleanup()
    {
        RefitterTelemetryTask.ResetTestHooks();
        Directory.Delete(workspace, true);
    }

    [TestMethod]
    public void Execute_Sends_Telemetry_When_Enabled()
    {
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));

        task.Execute().Should().BeTrue();

        sentPayloads.Should().ContainSingle();
        File.Exists(task.StampFile).Should().BeTrue();
    }

    [TestMethod]
    public void Execute_Skips_When_RefitterNoLogging_Is_Set()
    {
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));
        task.DisableLogging = true;

        task.Execute().Should().BeTrue();

        sentPayloads.Should().BeEmpty();
        File.Exists(task.StampFile).Should().BeFalse();
    }

    [TestMethod]
    public void Execute_Skips_During_Design_Time_Build()
    {
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));
        task.DesignTimeBuild = true;

        task.Execute().Should().BeTrue();

        sentPayloads.Should().BeEmpty();
        File.Exists(task.StampFile).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("1")]
    [DataRow("true")]
    [DataRow("TRUE")]
    [DataRow(" true ")]
    public void Execute_Skips_When_Dotnet_Telemetry_Opt_Out_Is_Set(string optOut)
    {
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));
        task.TelemetryOptOut = optOut;

        task.Execute().Should().BeTrue();

        sentPayloads.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("0")]
    [DataRow("false")]
    public void IsOptOut_Returns_False_For_Other_Values(string? optOut) =>
        RefitterTelemetryTask.IsOptOut(optOut).Should().BeFalse();

    [TestMethod]
    public void Execute_Skips_When_There_Are_No_Settings_Files()
    {
        var task = CreateTask();

        task.Execute().Should().BeTrue();

        sentPayloads.Should().BeEmpty();
    }

    [TestMethod]
    public void Execute_Only_Sends_Again_When_Settings_Files_Change()
    {
        var file = CreateSettingsFile("petstore.refitter");

        CreateTask(file).Execute();
        CreateTask(file).Execute();
        sentPayloads.Should().HaveCount(1);

        File.WriteAllText(file, """{ "openApiPath": "./other.json" }""");
        CreateTask(file).Execute();
        sentPayloads.Should().HaveCount(2);

        CreateTask(file, CreateSettingsFile("other.refitter")).Execute();
        sentPayloads.Should().HaveCount(3);
    }

    [TestMethod]
    public void Execute_Counts_Case_Distinct_Paths_As_Separate_Files()
    {
        if (!IsCaseSensitive(workspace))
        {
            Assert.Inconclusive("Requires a case-sensitive file system");
        }

        CreateTask(CreateSettingsFile("api.refitter"), CreateSettingsFile("API.refitter")).Execute();

        sentPayloads.Single().Last().Properties["file-count"].Should().Be("2");
    }

    [TestMethod]
    public void Execute_Does_Not_Fail_The_Build_When_Sending_Fails()
    {
        sender = (_, _) => throw new HttpRequestException("offline");
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));

        task.Execute().Should().BeTrue();

        File.Exists(task.StampFile).Should().BeTrue();
    }

    [TestMethod]
    public void Execute_Passes_Timeout_To_Sender()
    {
        int? timeout = null;
        sender = (_, value) => timeout = value;
        var task = CreateTask(CreateSettingsFile("petstore.refitter"));
        task.TimeoutMilliseconds = 1234;

        task.Execute();

        timeout.Should().Be(1234);
    }

    [TestMethod]
    public void Payload_Contains_Settings_And_Invocation_Events()
    {
        CreateTask(CreateSettingsFile("a.refitter"), CreateSettingsFile("b.refitter")).Execute();

        var events = sentPayloads.Single();
        events.Select(e => e.Name).Should().Equal("settings-file", "settings-file", "sourcegenerator-invocation");
        var settings = events[0].Properties["settings"];
        settings.Should().Contain("\"openApiPath\":\"<redacted>\"");
        events.SelectMany(e => e.Properties.Values).Should().NotContain(value => value.Contains("openapi.json"));
        events[2].Properties["file-count"].Should().Be("2");
    }

    private RefitterTelemetryTask CreateTask(params string[] settingsFiles)
    {
        // The sender hook is AsyncLocal, so it is set from within the test method
        RefitterTelemetryTask.Sender = sender ?? ((payload, _) => sentPayloads.Add(payload));
        return new()
        {
            BuildEngine = new NullBuildEngine(),
            SettingsFiles = settingsFiles.Select(file => new TaskItem(file)).ToArray<ITaskItem>(),
            StampFile = Path.Combine(workspace, "obj", "refitter.sourcegenerator.telemetry"),
        };
    }

    private static bool IsCaseSensitive(string folder)
    {
        var probe = Path.Combine(folder, "case-probe");
        File.WriteAllText(probe, string.Empty);
        try
        {
            return !File.Exists(probe.ToUpperInvariant());
        }
        finally
        {
            File.Delete(probe);
        }
    }

    private string CreateSettingsFile(string name)
    {
        var path = Path.Combine(workspace, name);
        File.WriteAllText(path, """{ "openApiPath": "./openapi.json", "namespace": "Te\"st" }""");
        return path;
    }

    private sealed class NullBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => string.Empty;

        public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs) => true;
        public void LogCustomEvent(CustomBuildEventArgs e) { }
        public void LogErrorEvent(BuildErrorEventArgs e) { }
        public void LogMessageEvent(BuildMessageEventArgs e) { }
        public void LogWarningEvent(BuildWarningEventArgs e) { }
    }
}
