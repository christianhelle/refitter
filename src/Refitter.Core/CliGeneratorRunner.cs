using System.Diagnostics;

namespace Refitter.Core;

/// <summary>
/// CLI-based implementation of IGeneratorRunner that spawns the Refitter CLI process.
/// </summary>
public class CliGeneratorRunner : IGeneratorRunner
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";
    internal const int DefaultProcessTimeoutMilliseconds = 300000;

    private readonly string refitterDll;
    private readonly int processTimeoutMilliseconds;

    public CliGeneratorRunner(string refitterDll, int processTimeoutMilliseconds = DefaultProcessTimeoutMilliseconds)
    {
        this.refitterDll = refitterDll;
        this.processTimeoutMilliseconds = processTimeoutMilliseconds;
    }

    public async Task<IReadOnlyList<string>> RunAsync(
        RefitGeneratorSettings settings,
        bool skipValidation,
        bool noLogging,
        CancellationToken cancellationToken)
    {
        var outputLines = new List<string>();

        var args = $"{refitterDll} --settings-file \"{settings.OpenApiPath}\" --simple-output";
        if (noLogging)
        {
            args += " --no-logging";
        }
        if (skipValidation)
        {
            args += " --skip-validation";
        }

        var processDirectory = Path.GetDirectoryName(settings.OpenApiPath) ?? Directory.GetCurrentDirectory();

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                WorkingDirectory = processDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        var outputLock = new object();
        process.OutputDataReceived += (sender, dataArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(dataArgs.Data))
            {
                lock (outputLock)
                {
                    outputLines.Add(dataArgs.Data!);
                }
            }
        };

        process.ErrorDataReceived += (sender, dataArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(dataArgs.Data))
            {
                lock (outputLock)
                {
                    outputLines.Add(dataArgs.Data!);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(processTimeoutMilliseconds);

        if (!exited)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Ignore kill failures
            }

            throw new TimeoutException(
                $"Refitter process timed out after {FormatTimeout(processTimeoutMilliseconds)} and was terminated");
        }

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var errorOutput = string.Join("\n", outputLines.Where(line => line!.StartsWith("Error:", StringComparison.Ordinal) || line!.StartsWith("ERROR:", StringComparison.Ordinal)));
            throw new InvalidOperationException(
                $"Refitter process exited with code {process.ExitCode}{(string.IsNullOrEmpty(errorOutput) ? "" : $": {errorOutput}")}");
        }

        return ResolveGeneratedFiles(outputLines, settings.OpenApiPath!);
    }

    private static string FormatTimeout(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds < 1000)
            return $"{timeoutMilliseconds} ms";

        if (timeoutMilliseconds % 1000 == 0)
            return $"{timeoutMilliseconds / 1000} seconds";

        return $"{timeoutMilliseconds / 1000d:0.###} seconds";
    }

    internal static List<string> ResolveGeneratedFiles(IEnumerable<string?> outputLines, string settingsFilePath)
    {
        var existingGeneratedFiles = outputLines
            .Select(ParseGeneratedFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (existingGeneratedFiles.Count == 0)
        {
            throw new InvalidOperationException(
                $"Refitter did not report any generated files for {settingsFilePath}");
        }

        return existingGeneratedFiles;
    }

    internal static string? ParseGeneratedFilePath(string? outputLine)
    {
        var markerLine = outputLine ?? string.Empty;
        if (string.IsNullOrWhiteSpace(markerLine))
            return null;

        if (!markerLine.StartsWith(GeneratedFileMarker, StringComparison.Ordinal))
            return null;

        var generatedFilePath = markerLine.Substring(GeneratedFileMarker.Length).Trim();
        return string.IsNullOrWhiteSpace(generatedFilePath) ? null : generatedFilePath;
    }
}
