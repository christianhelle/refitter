using Exceptionless;
using FluentAssertions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Refitter.Core;

namespace Refitter.Tests.Telemetry;


public class AnalyticsTests
{
    [Test]
    [NotInParallel("Telemetry")]
    public void Configure_Should_Not_Throw()
    {
        var action = () => Analytics.Configure();

        action.Should().NotThrow();
    }

    [Test]
    public void LogFeatureUsage_Should_Skip_When_NoLogging_Is_True()
    {
        var settings = new Settings
        {
            NoLogging = true,
            ReturnIApiResponse = true
        };
        var refitSettings = new RefitGeneratorSettings();

        var action = () => Analytics.LogFeatureUsage(settings, refitSettings);

        action.Should().NotThrow();
    }

    [Test]
    public async Task LogError_Should_Skip_When_NoLogging_Is_True()
    {
        var settings = new Settings
        {
            NoLogging = true,
            OpenApiPath = "test.json"
        };
        var exception = new Exception("Test exception");

        var action = async () => await Analytics.LogError(exception, settings);

        await action.Should().NotThrowAsync();
    }

    [Test]
    public void LogFeatureUsage_Should_Handle_Null_Properties()
    {
        var settings = new Settings
        {
            NoLogging = true,
            OpenApiPath = null,
            SettingsFilePath = null,
            ContractsNamespace = null,
            OperationNameTemplate = null
        };
        var refitSettings = new RefitGeneratorSettings();

        var action = () => Analytics.LogFeatureUsage(settings, refitSettings);

        action.Should().NotThrow();
    }

    [Test]
    public void IsMsbuildInvocation_Should_Be_True_Only_For_Msbuild_Source()
    {
        Analytics.IsMsbuildInvocation("msbuild").Should().BeTrue();
        Analytics.IsMsbuildInvocation("MSBUILD").Should().BeTrue();
        Analytics.IsMsbuildInvocation("cli").Should().BeFalse();
        Analytics.IsMsbuildInvocation("").Should().BeFalse();
        Analytics.IsMsbuildInvocation(null).Should().BeFalse();
    }

    [Test]
    [NotInParallel("Telemetry")]
    public void LogFeatureUsage_Should_Track_Msbuild_Invocation_With_Telemetry_Settings()
    {
        var captured = new List<ITelemetry>();
        var client = CreateTelemetryClient(captured);
        Analytics.SetTelemetryClient(client);

        var settings = new Settings
        {
            TelemetrySource = "msbuild",
            TelemetryFileCount = 3,
            TelemetryRuntime = "net9.0"
        };
        var refitSettings = new RefitGeneratorSettings();

        Analytics.LogFeatureUsage(settings, refitSettings);

        var msbuildEvent = captured
            .OfType<EventTelemetry>()
            .Single(e => e.Name == "msbuild-invocation");
        msbuildEvent.Properties["file-count"].Should().Be("3");
        msbuildEvent.Properties["runtime"].Should().Be("net9.0");
        client.Context.GlobalProperties["telemetry-source"].Should().Be("msbuild");
    }

    [Test]
    [NotInParallel("Telemetry")]
    public async Task LogError_Should_Apply_Msbuild_Telemetry_Source()
    {
        ExceptionlessClient.Default.Configuration.Enabled = false;
        var captured = new List<ITelemetry>();
        var client = CreateTelemetryClient(captured);
        Analytics.SetTelemetryClient(client);

        var settings = new Settings
        {
            TelemetrySource = "msbuild",
            TelemetryFileCount = 3,
            TelemetryRuntime = "net9.0"
        };
        var exception = new Exception("Test exception");

        await Analytics.LogError(exception, settings);

        captured.OfType<ExceptionTelemetry>().Should().NotBeEmpty();
        client.Context.GlobalProperties["telemetry-source"].Should().Be("msbuild");
    }

    private static TelemetryClient CreateTelemetryClient(List<ITelemetry> captured)
    {
        var configuration = new TelemetryConfiguration
        {
            ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
            TelemetryChannel = new CapturingTelemetryChannel(captured)
        };
        return new TelemetryClient(configuration);
    }

    private sealed class CapturingTelemetryChannel : ITelemetryChannel
    {
        private readonly List<ITelemetry> captured;

        public CapturingTelemetryChannel(List<ITelemetry> captured) =>
            this.captured = captured;

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; } = string.Empty;

        public void Send(ITelemetry item) => captured.Add(item);

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
