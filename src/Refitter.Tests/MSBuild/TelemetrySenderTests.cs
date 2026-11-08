using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Refitter.SourceGenerator.Telemetry;

namespace Refitter.Tests.MSBuild;

[TestClass]
public class TelemetrySenderTests
{
    [TestMethod]
    public void Send_Tracks_Events_With_The_Same_Context_As_The_CLI()
    {
        var channel = new RecordingChannel();
        var events = TelemetryPayload.Create(["""{ "generateContracts": false }"""]);

        TelemetrySender.Send(events, 1000, channel, "InstrumentationKey=00000000-0000-0000-0000-000000000000");

        var tracked = channel.Items.Cast<EventTelemetry>().ToList();
        tracked.Select(e => e.Name).Should().Equal("settings-file", "sourcegenerator-invocation");
        tracked[0].Properties["settings"].Should().Be("""{"generateContracts":false}""");
        tracked[1].Properties["file-count"].Should().Be("1");
        tracked.Should().OnlyContain(e =>
            e.Context.GlobalProperties["telemetry-source"] == "sourcegenerator" &&
            e.Context.GlobalProperties["support-key"] == TelemetryPayload.GetSupportKey() &&
            e.Context.User.Id == TelemetryPayload.GetSupportKey() &&
            e.Context.Cloud.RoleInstance == TelemetryPayload.GetSupportKey() &&
            e.Context.Component.Version == TelemetryPayload.GetVersion());
    }

    [TestMethod]
    public void Send_Gives_Up_After_The_Timeout_When_The_Endpoint_Does_Not_Respond()
    {
        // A local listener that accepts connections but never answers
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var events = TelemetryPayload.Create(["""{ "generateContracts": false }"""]);
        var stopwatch = Stopwatch.StartNew();

        var send = () => TelemetrySender.Send(
            events,
            500,
            new InMemoryChannel(),
            $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:{port}/");

        send.Should().Throw<TimeoutException>();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
    }

    private sealed class RecordingChannel : ITelemetryChannel
    {
        public List<ITelemetry> Items { get; } = [];

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress { get; set; } = string.Empty;

        public void Send(ITelemetry item) => Items.Add(item);

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}
