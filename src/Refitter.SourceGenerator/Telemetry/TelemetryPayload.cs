using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>An Application Insights custom event.</summary>
internal sealed class TelemetryEvent(string name, IDictionary<string, string> properties)
{
    public string Name { get; } = name;

    public IDictionary<string, string> Properties { get; } = properties;
}

/// <summary>
/// Builds the same events the Refitter CLI sends, tagged as coming from the source generator.
/// </summary>
internal static class TelemetryPayload
{
    internal const string TelemetrySource = "sourcegenerator";
    internal const string InvocationEventName = "sourcegenerator-invocation";
    internal const string SettingsFileEventName = "settings-file";

    public static IReadOnlyList<TelemetryEvent> Create(IReadOnlyList<string> settingsFileContents)
    {
        var events = settingsFileContents
            .Select(contents => new TelemetryEvent(SettingsFileEventName, CreateSettingsProperties(contents)))
            .ToList();

        events.Add(
            new TelemetryEvent(
                InvocationEventName,
                new Dictionary<string, string>
                {
                    { "file-count", settingsFileContents.Count.ToString(CultureInfo.InvariantCulture) }
                }));

        return events;
    }

    // Only the sanitized settings are sent, never the raw file contents
    private static Dictionary<string, string> CreateSettingsProperties(string contents)
    {
        var properties = new Dictionary<string, string>();
        var settings = SettingsSanitizer.Sanitize(contents);
        if (settings is not null)
        {
            properties["settings"] = settings;
        }

        return properties;
    }

    public static string GetVersion() =>
        typeof(TelemetryPayload).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    internal static string GetSupportKey() => GetAnonymousIdentity().Substring(0, 7);

    private static string GetAnonymousIdentity()
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{Environment.UserName}@{GetMachineName()}"));
        return Convert.ToBase64String(bytes).ToLowerInvariant();
    }

    private static string GetMachineName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch (Exception)
        {
            return "localhost";
        }
    }
}
