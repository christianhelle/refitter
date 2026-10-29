using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>
/// Builds the Application Insights track request for the same events the Refitter CLI sends,
/// without taking a dependency on the Application Insights SDK.
/// </summary>
internal static class TelemetryPayload
{
    internal const string TelemetrySource = "sourcegenerator";
    internal const string InvocationEventName = "sourcegenerator-invocation";
    internal const string SettingsFileEventName = "settings-file";

    // Same Application Insights resource as the Refitter CLI (src/Refitter/Analytics.cs)
    private const string InstrumentationKey = "470c204f-b460-493a-9e31-d9b2f5e25abb";

    public static string Create(IReadOnlyList<string> settingsFileContents, string version)
    {
        var supportKey = GetSupportKey();
        var tags = new Dictionary<string, string>
        {
            { "ai.user.id", supportKey },
            { "ai.session.id", Guid.NewGuid().ToString() },
            { "ai.operation.id", Guid.NewGuid().ToString() },
            { "ai.device.osVersion", Environment.OSVersion.ToString() },
            { "ai.application.ver", version },
        };

        var time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        var envelopes = settingsFileContents
            .Select(contents => CreateEnvelope(
                SettingsFileEventName,
                CreateSettingsProperties(contents),
                tags,
                supportKey,
                time))
            .ToList();

        envelopes.Add(
            CreateEnvelope(
                InvocationEventName,
                new Dictionary<string, string>
                {
                    { "file-count", settingsFileContents.Count.ToString(CultureInfo.InvariantCulture) }
                },
                tags,
                supportKey,
                time));

        return "[" + string.Join(",", envelopes) + "]";
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

    private static string CreateEnvelope(
        string eventName,
        Dictionary<string, string> properties,
        Dictionary<string, string> tags,
        string supportKey,
        string time)
    {
        properties["telemetry-source"] = TelemetrySource;
        properties["support-key"] = supportKey;

        var builder = new StringBuilder();
        builder.Append("{\"name\":\"Microsoft.ApplicationInsights.")
            .Append(InstrumentationKey.Replace("-", string.Empty))
            .Append(".Event\",\"time\":").Append(Quote(time))
            .Append(",\"iKey\":").Append(Quote(InstrumentationKey))
            .Append(",\"tags\":").Append(ToJsonObject(tags))
            .Append(",\"data\":{\"baseType\":\"EventData\",\"baseData\":{\"ver\":2,\"name\":")
            .Append(Quote(eventName))
            .Append(",\"properties\":").Append(ToJsonObject(properties))
            .Append("}}}");
        return builder.ToString();
    }

    private static string ToJsonObject(Dictionary<string, string> values) =>
        "{" + string.Join(",", values.Select(pair => Quote(pair.Key) + ":" + Quote(pair.Value))) + "}";

    internal static string Quote(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.Append('"').ToString();
    }
}
