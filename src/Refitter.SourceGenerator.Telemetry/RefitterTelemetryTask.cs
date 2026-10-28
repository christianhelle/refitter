using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>
/// Reports anonymous usage telemetry for Refitter.SourceGenerator.
/// This runs as an MSBuild task so that the source generator itself never makes network calls.
/// Telemetry is only sent when the .refitter files or the package version change and the task never fails the build.
/// </summary>
public class RefitterTelemetryTask : MSBuildTask
{
    private const string IngestionEndpoint = "https://westeurope-5.in.applicationinsights.azure.com/v2/track";
    private static readonly AsyncLocal<Action<string, int>?> SenderOverride = new();

    internal static Action<string, int> Sender
    {
        get => SenderOverride.Value ?? Post;
        set => SenderOverride.Value = value;
    }

    public ITaskItem[] SettingsFiles { get; set; } = [];

    [Required]
    public string StampFile { get; set; } = string.Empty;

    public bool DesignTimeBuild { get; set; }

    public bool DisableLogging { get; set; }

    public string? TelemetryOptOut { get; set; }

    public int TimeoutMilliseconds { get; set; } = 3000;

    internal static void ResetTestHooks() => SenderOverride.Value = null;

    public override bool Execute()
    {
        try
        {
            TrySendTelemetry();
        }
        catch (Exception e)
        {
            TryLogMessage($"Skipped Refitter telemetry: {e.Message}");
        }

        return true;
    }

    internal static bool IsEnabled(bool designTimeBuild, bool disableLogging, string? telemetryOptOut) =>
        !designTimeBuild && !disableLogging && !IsOptOut(telemetryOptOut);

    internal static bool IsOptOut(string? telemetryOptOut) =>
        telemetryOptOut is not null &&
        (telemetryOptOut.Trim() == "1" ||
         string.Equals(telemetryOptOut.Trim(), "true", StringComparison.OrdinalIgnoreCase));

    internal static string ComputeFingerprint(string version, IReadOnlyList<string> files, IReadOnlyList<string> contents)
    {
        var builder = new StringBuilder(version);
        for (var i = 0; i < files.Count; i++)
        {
            builder.Append('\n').Append(files[i]).Append('\n').Append(contents[i]);
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private void TrySendTelemetry()
    {
        if (!IsEnabled(DesignTimeBuild, DisableLogging, TelemetryOptOut))
        {
            return;
        }

        var files = SettingsFiles
            .Select(item => item.GetMetadata("FullPath"))
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (files.Length == 0)
        {
            return;
        }

        var contents = files.Select(File.ReadAllText).ToArray();
        var version = TelemetryPayload.GetVersion();
        var fingerprint = ComputeFingerprint(version, files, contents);
        if (File.Exists(StampFile) && File.ReadAllText(StampFile).Trim() == fingerprint)
        {
            return;
        }

        // Write the stamp before sending so that an offline machine doesn't retry on every build
        var stampFolder = Path.GetDirectoryName(StampFile);
        if (!string.IsNullOrWhiteSpace(stampFolder))
        {
            Directory.CreateDirectory(stampFolder);
        }

        File.WriteAllText(StampFile, fingerprint);

        TryLogMessage($"Sending Refitter telemetry for {files.Length} .refitter files");
        Sender(TelemetryPayload.Create(contents, version), TimeoutMilliseconds);
    }

    private static void Post(string payload, int timeoutMilliseconds)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = client.PostAsync(IngestionEndpoint, content).GetAwaiter().GetResult();
    }

    private void TryLogMessage(string text)
    {
        try
        {
            Log.LogMessage(MessageImportance.Low, text);
        }
        catch (Exception)
        {
            // Logging is best effort, telemetry must never fail the build
        }
    }
}
