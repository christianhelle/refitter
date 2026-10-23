namespace Refitter.Core;

/// <summary>
/// The signature of a generated Refit method: the route template it binds to, its parameters, and any
/// dynamic querystring or multipart form object wrapper type source that must be emitted for the operation.
/// </summary>
#pragma warning disable S1186 // Positional record constructor is compiler-generated
internal sealed record MethodSignature(
#pragma warning restore S1186
    string Path,
    string ParametersString,
    IReadOnlyList<string> Parameters,
    string? DynamicQuerystringParameters)
{
    public string? FormObjectParameters { get; init; }
}
