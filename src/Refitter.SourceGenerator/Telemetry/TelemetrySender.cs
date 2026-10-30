using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>
/// Sends telemetry with the Application Insights SDK, configured like the Refitter CLI (src/Refitter/Analytics.cs).
/// </summary>
internal static class TelemetrySender
{
    // Same Application Insights resource as the Refitter CLI
    private const string ConnectionString = "InstrumentationKey=470c204f-b460-493a-9e31-d9b2f5e25abb;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=0836c3ac-e8ac-4e0c-ade8-3e0fadb9b40c";

    public static void Send(IReadOnlyList<TelemetryEvent> events, int timeoutMilliseconds) =>
        Send(events, timeoutMilliseconds, new InMemoryChannel(), ConnectionString);

    internal static void Send(
        IReadOnlyList<TelemetryEvent> events,
        int timeoutMilliseconds,
        ITelemetryChannel channel,
        string connectionString)
    {
        // Set the channel before the connection string so that the channel gets its ingestion endpoint
        using var configuration = new TelemetryConfiguration();
        configuration.TelemetryChannel = channel;
        configuration.ConnectionString = connectionString;

        var supportKey = TelemetryPayload.GetSupportKey();
        var client = new TelemetryClient(configuration);
        client.Context.User.Id = supportKey;
        client.Context.Session.Id = Guid.NewGuid().ToString();
        client.Context.Operation.Id = Guid.NewGuid().ToString();
        client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

        // The SDK reports the machine name as the role instance by default; send the anonymous key instead
        client.Context.Cloud.RoleInstance = supportKey;
        client.Context.Component.Version = TelemetryPayload.GetVersion();
        client.Context.GlobalProperties["telemetry-source"] = TelemetryPayload.TelemetrySource;
        client.Context.GlobalProperties["support-key"] = supportKey;

        foreach (var telemetryEvent in events)
        {
            client.TrackEvent(telemetryEvent.Name, telemetryEvent.Properties);
        }

        // The channel waits up to 100 seconds for the endpoint and ignores cancellation once a request is sent,
        // so wait for the flush ourselves and give up after the timeout rather than holding up the build
        var flush = client.FlushAsync(CancellationToken.None);
        if (!flush.Wait(timeoutMilliseconds))
        {
            throw new TimeoutException($"Sending telemetry did not complete within {timeoutMilliseconds} ms");
        }
    }
}
