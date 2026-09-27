namespace Refitter.Core.Validation.Model;

/// <summary>
/// The parts of an OpenAPI document that validation rules and statistics look at. It mirrors the object model
/// of Microsoft.OpenApi (MIT license): a Swagger 2.0 document is read into the same OpenAPI 3 shape.
/// </summary>
internal sealed class SpecDocument
{
    public SpecInfo? Info { get; set; } = new();

    public List<SpecServer>? Servers { get; set; }

    public SpecPaths? Paths { get; set; } = new();

    public Dictionary<string, SpecPathItem?>? Webhooks { get; set; }

    public SpecComponents? Components { get; set; }

    public List<SpecSecurityRequirement>? Security { get; set; }

    public SpecExternalDocs? ExternalDocs { get; set; }

    public List<SpecTag>? Tags { get; set; }
}

internal sealed class SpecInfo
{
    public string? Title { get; set; }

    public string? Version { get; set; }

    public SpecContact? Contact { get; set; }

    public SpecLicense? License { get; set; }
}

internal sealed class SpecContact
{
    public string? Email { get; set; }
}

internal sealed class SpecLicense
{
    public string? Name { get; set; }
}

internal sealed class SpecServer
{
    public string? Url { get; set; }

    public Dictionary<string, SpecServerVariable?>? Variables { get; set; }
}

internal sealed class SpecServerVariable
{
    public string? Default { get; set; }
}

internal sealed class SpecExternalDocs
{
    public Uri? Url { get; set; }
}

internal sealed class SpecTag
{
    public string? Name { get; set; }

    public SpecExternalDocs? ExternalDocs { get; set; }
}

internal sealed class SpecComponents
{
    public Dictionary<string, SpecSchema?>? Schemas { get; set; }

    public Dictionary<string, SpecResponse?>? Responses { get; set; }

    public Dictionary<string, SpecParameter?>? Parameters { get; set; }

    public Dictionary<string, SpecExample?>? Examples { get; set; }

    public Dictionary<string, SpecRequestBody?>? RequestBodies { get; set; }

    public Dictionary<string, SpecHeader?>? Headers { get; set; }

    public Dictionary<string, SpecSecurityScheme?>? SecuritySchemes { get; set; }

    public Dictionary<string, SpecLink?>? Links { get; set; }

    public Dictionary<string, SpecCallback?>? Callbacks { get; set; }

    public Dictionary<string, SpecPathItem?>? PathItems { get; set; }

    public Dictionary<string, SpecMediaType?>? MediaTypes { get; set; }

    /// <summary>
    /// Copies the component maps, so components added to this document later are not in the copy.
    /// </summary>
    public SpecComponents Copy() =>
        new()
        {
            Schemas = Copy(Schemas),
            Responses = Copy(Responses),
            Parameters = Copy(Parameters),
            Examples = Copy(Examples),
            RequestBodies = Copy(RequestBodies),
            Headers = Copy(Headers),
            SecuritySchemes = Copy(SecuritySchemes),
            Links = Copy(Links),
            Callbacks = Copy(Callbacks),
            PathItems = Copy(PathItems),
            MediaTypes = Copy(MediaTypes),
        };

    private static Dictionary<string, T?>? Copy<T>(Dictionary<string, T?>? components) =>
        components == null ? null : new Dictionary<string, T?>(components, StringComparer.Ordinal);
}
