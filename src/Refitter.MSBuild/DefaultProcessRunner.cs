using System;
using System.Diagnostics;

namespace Refitter.MSBuild;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that uses <see cref="Process"/>
/// </summary>
public sealed class DefaultProcessRunner : IProcessRunner
{
    private static readonly int DefaultTimeoutMilliseconds = 300000;

    /// <summary>
    /// Gets or sets the process timeout in milliseconds
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = DefaultTimeoutMilliseconds;

    /// <summary>
    /// Gets or sets the action to terminate a timed-out process
    /// </summary>
    public Action<Process> ProcessTerminator { get; set; } = DefaultTerminateProcess;

    /// <inheritdoc />
    public ProcessExecutionResult Run(
        ProcessStartInfo startInfo,
        Action<string?> handleStandardOutput,
        Action<string?> handleErrorOutput)
    {
        using var process = new Process();
        process.StartInfo = startInfo;

        process.ErrorDataReceived += (_, args) => handleErrorOutput(args.Data);
        process.OutputDataReceived += (_, args) => handleStandardOutput(args.Data);
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        if (!process.WaitForExit(TimeoutMilliseconds))
        {
            try
            {
                ProcessTerminator(process);
                return new ProcessExecutionResult(true, -1);
            }
            catch (Exception ex)
            {
                return new ProcessExecutionResult(true, -1, ex);
            }
        }

        process.WaitForExit();
        return new ProcessExecutionResult(false, process.ExitCode);
    }

    private static void DefaultTerminateProcess(Process process) => process.Kill();
}
