using System;
using System.Collections.Generic;

namespace Refitter.Core;

public record RunResult(
    IReadOnlyList<PlannedFile> GeneratedFiles,
    IReadOnlyList<Warning> Warnings,
    IReadOnlyList<RunnerDiagnostic> Diagnostics,
    TimeSpan Elapsed,
    int ExitCode,
    Exception? Exception = null);
