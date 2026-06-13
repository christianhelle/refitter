using System;

namespace Refitter.MSBuild;

/// <summary>
/// Represents the result of executing a process
/// </summary>
public sealed class ProcessExecutionResult(
    bool timedOut,
    int exitCode,
    Exception? terminationException = null)
{
    /// <summary>
    /// Whether the process timed out
    /// </summary>
    public bool TimedOut { get; } = timedOut;

    /// <summary>
    /// The exit code of the process
    /// </summary>
    public int ExitCode { get; } = exitCode;

    /// <summary>
    /// Exception thrown during process termination, if any
    /// </summary>
    public Exception? TerminationException { get; } = terminationException;
}
