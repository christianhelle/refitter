namespace Refitter.Core.Validation;

/// <summary>
/// An error or warning found while validating an OpenAPI document.
/// </summary>
/// <param name="Pointer">The JSON pointer to the offending element, when known.</param>
/// <param name="Message">The description of the issue.</param>
#pragma warning disable S1186 // Positional record constructor is compiler-generated
public sealed record ValidationIssue(string? Pointer, string Message)
#pragma warning restore S1186
{
    /// <summary>
    /// Returns the message, followed by the pointer in brackets when there is one.
    /// </summary>
    public override string ToString() =>
        string.IsNullOrEmpty(Pointer) ? Message : $"{Message} [{Pointer}]";
}
