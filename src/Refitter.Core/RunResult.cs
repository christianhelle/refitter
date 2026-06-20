using System;
using System.Collections.Generic;

namespace Refitter.Core;

/// <summary>
/// Represents the result of a <see cref="RefitterRunner.RunAsync"/> invocation.
/// </summary>
/// <param name="GeneratedFiles">The list of planned output files with their paths and content.</param>
/// <param name="Warnings">Configuration warnings detected during the run.</param>
/// <param name="Diagnostics">Validation and error diagnostics collected during the run.</param>
/// <param name="Elapsed">The elapsed time of the run.</param>
/// <param name="ExitCode">The exit code (0 for success, non-zero for failure).</param>
/// <param name="Exception">The original exception, if any, for form-specific error handling.</param>
public record RunResult(
    IReadOnlyList<PlannedFile> GeneratedFiles,
    IReadOnlyList<Warning> Warnings,
    IReadOnlyList<RunnerDiagnostic> Diagnostics,
    TimeSpan Elapsed,
    int ExitCode,
    Exception? Exception = null);
