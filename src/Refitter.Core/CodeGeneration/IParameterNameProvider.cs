namespace Refitter.Core;

/// <summary>
/// Provides the C# parameter names of generated Refit interface methods.
/// </summary>
/// <remarks>
/// When a provided name differs from the name in the OpenAPI document, Refitter keeps the wire name
/// with an <c>AliasAs</c> or <c>Header</c> attribute.
/// </remarks>
public interface IParameterNameProvider
{
    /// <summary>
    /// Gets the C# parameter name for an operation parameter.
    /// </summary>
    /// <param name="context">The operation parameter to name.</param>
    /// <returns>A valid C# identifier.</returns>
    string GetParameterName(ParameterNameContext context);
}

/// <summary>
/// Describes an operation parameter that needs a C# parameter name.
/// </summary>
/// <param name="Name">The parameter name as it appears in the OpenAPI document.</param>
/// <param name="Source">Where the parameter is sent in the HTTP request.</param>
/// <param name="IsRequired">Whether the parameter is required.</param>
/// <param name="AllParameterNames">The names of all parameters of the operation, as they appear in the OpenAPI document.</param>
public sealed record ParameterNameContext(
    string Name,
    ParameterSource Source,
    bool IsRequired,
    IReadOnlyList<string> AllParameterNames);

/// <summary>
/// Where an operation parameter is sent in the HTTP request.
/// </summary>
public enum ParameterSource
{
    /// <summary>A path segment.</summary>
    Path,

    /// <summary>A query string parameter.</summary>
    Query,

    /// <summary>An HTTP header.</summary>
    Header,

    /// <summary>A cookie.</summary>
    Cookie,

    /// <summary>The request body.</summary>
    Body,

    /// <summary>A form field.</summary>
    Form,

    /// <summary>A location that is none of the above.</summary>
    Other,
}
