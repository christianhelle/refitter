namespace Refitter.Core;

/// <summary>
/// The ordered set of a generated Refit method's parameters, together with the route template
/// they bind to and any dynamic querystring or multipart form object wrapper type source that must be
/// emitted for the operation.
/// </summary>
#pragma warning disable S1186 // Positional record constructor is compiler-generated
internal sealed record ParameterList(
#pragma warning restore S1186
    string Path,
    IReadOnlyList<MethodParameter> Parameters,
    string? DynamicQuerystringCode)
{
    public string? FormObjectCode { get; init; }

    /// <summary>
    /// The parameters as they appear between the parentheses of the method declaration.
    /// </summary>
    public string Render() => MethodParameter.Render(Parameters);
}
