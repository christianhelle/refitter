namespace Refitter.Core.Validation;

/// <summary>
/// An error or warning found while validating an OpenAPI document.
/// </summary>
/// <param name="Pointer">The JSON pointer to the offending element, when known.</param>
/// <param name="Message">The description of the issue.</param>
public sealed record ValidationIssue(string? Pointer, string Message)
{
    /// <summary>
    /// Returns the message, followed by the pointer in brackets when there is one.
    /// </summary>
    public override string ToString() =>
        string.IsNullOrEmpty(Pointer) ? Message : $"{Message} [{Pointer}]";
}
