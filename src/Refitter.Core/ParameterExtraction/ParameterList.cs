namespace Refitter.Core;

/// <summary>
/// The ordered set of a generated Refit method's parameters, together with the route template
/// they bind to and any dynamic querystring or multipart form object wrapper type source that must be
/// emitted for the operation.
/// </summary>
internal sealed record ParameterList(
    string Path,
    IReadOnlyList<string> Parameters,
    string? DynamicQuerystringCode)
{
    public string? FormObjectCode { get; init; }
}
