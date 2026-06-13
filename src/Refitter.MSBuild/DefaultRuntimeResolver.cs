using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Refitter.MSBuild;

/// <summary>
/// Default implementation of <see cref="IRuntimeResolver"/> that runs 'dotnet --list-runtimes'
/// </summary>
public sealed class DefaultRuntimeResolver : IRuntimeResolver
{
    private readonly IProcessRunner processRunner;

    /// <summary>
    /// Gets or sets the timeout in milliseconds used for error message formatting
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = RefitterGenerateTask.DefaultProcessTimeoutMilliseconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRuntimeResolver"/> class.
    /// </summary>
    public DefaultRuntimeResolver(IProcessRunner processRunner)
    {
        this.processRunner = processRunner;
    }

    /// <inheritdoc />
    public List<string> GetInstalledRuntimes()
    {
        var installedRuntimes = new List<string>();
        var errorLines = new List<string>();
        var processResult = processRunner.Run(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            data => AddProcessOutputLine(data, installedRuntimes),
            data => AddProcessOutputLine(data, errorLines));

        if (processResult.TimedOut)
        {
            var timeoutDescription = FormatTimeout(TimeoutMilliseconds);
            if (processResult.TerminationException is null)
            {
                throw new TimeoutException($"dotnet --list-runtimes timed out after {timeoutDescription}");
            }

            throw new TimeoutException(
                $"dotnet --list-runtimes timed out after {timeoutDescription}. Failed to terminate timed-out process: {processResult.TerminationException.Message}",
                processResult.TerminationException);
        }

        if (processResult.ExitCode != 0)
        {
            var errorDetails = errorLines.Count > 0
                ? $"{Environment.NewLine}{string.Join(Environment.NewLine, errorLines)}"
                : string.Empty;
            throw new InvalidOperationException($"dotnet --list-runtimes exited with code {processResult.ExitCode}{errorDetails}");
        }

        return installedRuntimes;
    }

    internal static string FormatTimeout(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds < 1000)
            return $"{timeoutMilliseconds} ms";

        if (timeoutMilliseconds % 1000 == 0)
            return $"{timeoutMilliseconds / 1000} seconds";

        return $"{timeoutMilliseconds / 1000d:0.###} seconds";
    }

    private static void AddProcessOutputLine(string? outputLine, ICollection<string> outputLines)
    {
        if (!string.IsNullOrWhiteSpace(outputLine))
            outputLines.Add(outputLine!);
    }
}
