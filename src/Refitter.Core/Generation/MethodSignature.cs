namespace Refitter.Core;

/// <summary>
/// The signature of a generated Refit method: the route template it binds to, its parameters, and any
/// dynamic querystring wrapper type source that must be emitted for the operation.
/// </summary>
internal sealed record MethodSignature(
    string Path,
    string ParametersString,
    IReadOnlyList<string> Parameters,
    string? DynamicQuerystringParameters);
