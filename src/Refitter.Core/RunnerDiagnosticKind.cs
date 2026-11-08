namespace Refitter.Core;

/// <summary>
/// Identifies a <see cref="RunnerDiagnostic"/>, so hosts can react to it without matching its text.
/// </summary>
public enum RunnerDiagnosticKind
{
    /// <summary>A diagnostic without a specific kind, like a validation or generation error.</summary>
    Other,

    /// <summary>The include path patterns filtered out every path of the OpenAPI document.</summary>
    AllPathsFiltered,
}
