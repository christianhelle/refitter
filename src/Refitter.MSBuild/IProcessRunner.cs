using System;
using System.Diagnostics;

namespace Refitter.MSBuild;

/// <summary>
/// Abstraction for running a process
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process with the specified start info and output handlers
    /// </summary>
    ProcessExecutionResult Run(
        ProcessStartInfo startInfo,
        Action<string?> standardOutput,
        Action<string?> standardError);
}
