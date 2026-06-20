namespace Refitter.Core;

/// <summary>
/// Represents a diagnostic message from a generation run, such as a validation error or warning.
/// </summary>
/// <param name="Message">The diagnostic message text.</param>
/// <param name="IsError">Whether this diagnostic represents an error (true) or a warning (false).</param>
public record RunnerDiagnostic(string Message, bool IsError);
