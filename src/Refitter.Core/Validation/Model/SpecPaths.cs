using System.Text.Json.Nodes;

namespace Refitter.Core.Validation.Model;

/// <summary>
/// A <c>$ref</c> to a component: its id and, for a reference into another file, the file.
/// </summary>
internal sealed record SpecReference(string Id, string? ExternalResource)
{
    /// <summary>
    /// The JSON pointer an OpenAPI 3.1 schema reference was written with, when it is not a component.
    /// </summary>
    public string? JsonPointer { get; init; }
}

/// <summary>
/// An element that is either defined in place or, when <see cref="Reference"/> is set, only refers to one.
/// </summary>
internal abstract class SpecReferenceable
{
    public SpecReference? Reference { get; set; }
}

internal sealed class SpecPaths : Dictionary<string, SpecPathItem?>
{
}

internal sealed class SpecPathItem : SpecReferenceable
{
    public List<SpecParameter>? Parameters { get; set; }

    public Dictionary<string, SpecOperation>? Operations { get; set; }

    public void AddOperation(string method, SpecOperation operation)
    {
        Operations ??= new Dictionary<string, SpecOperation>(StringComparer.Ordinal);
        Operations[method] = operation;
    }
}

internal sealed class SpecOperation
{
    public List<SpecParameter>? Parameters { get; set; }

    public SpecRequestBody? RequestBody { get; set; }

    /// <summary>
    /// Starts empty, like in Microsoft.OpenApi, so an operation without responses breaks a validation rule.
    /// </summary>
    public SpecResponses? Responses { get; set; } = new();

    public Dictionary<string, SpecCallback?>? Callbacks { get; set; }

    public List<SpecSecurityRequirement>? Security { get; set; }
}

internal sealed class SpecParameter : SpecReferenceable
{
    public string? Name { get; set; }

    public SpecParameterLocation? In { get; set; }

    public bool Required { get; set; }

    public SpecSchema? Schema { get; set; }

    public Dictionary<string, SpecMediaType?>? Content { get; set; }

    public Dictionary<string, SpecExample?>? Examples { get; set; }
}

internal enum SpecParameterLocation
{
    Query,
    Header,
    Path,
    Cookie,
    QueryString,
}

internal sealed class SpecRequestBody : SpecReferenceable
{
    public Dictionary<string, SpecMediaType?>? Content { get; set; }
}

internal sealed class SpecMediaType : SpecReferenceable
{
    public SpecSchema? Schema { get; set; }

    public Dictionary<string, SpecExample?>? Examples { get; set; }

    public Dictionary<string, SpecEncoding?>? Encoding { get; set; }
}

internal sealed class SpecEncoding
{
    public Dictionary<string, SpecHeader?>? Headers { get; set; }
}

internal sealed class SpecResponses : Dictionary<string, SpecResponse?>
{
}

internal sealed class SpecResponse : SpecReferenceable
{
    public string? Description { get; set; }

    public Dictionary<string, SpecMediaType?>? Content { get; set; }

    public Dictionary<string, SpecLink?>? Links { get; set; }

    public Dictionary<string, SpecHeader?>? Headers { get; set; }
}

internal sealed class SpecHeader : SpecReferenceable
{
    public Dictionary<string, SpecMediaType?>? Content { get; set; }

    public JsonNode? Example { get; set; }

    public Dictionary<string, SpecExample?>? Examples { get; set; }

    public SpecSchema? Schema { get; set; }
}

internal sealed class SpecLink : SpecReferenceable
{
    public SpecServer? Server { get; set; }
}

internal sealed class SpecCallback : SpecReferenceable
{
    public Dictionary<string, SpecPathItem?>? PathItems { get; set; }
}

internal sealed class SpecExample : SpecReferenceable
{
}
