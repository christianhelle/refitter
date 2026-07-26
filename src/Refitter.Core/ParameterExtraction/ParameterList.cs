namespace Refitter.Core;

/// <summary>
/// The ordered set of a generated Refit method's parameters, together with any
/// dynamic querystring wrapper type source that must be emitted for the operation.
/// </summary>
internal sealed record ParameterList(
    IReadOnlyList<string> Parameters,
    string? DynamicQuerystringCode);
