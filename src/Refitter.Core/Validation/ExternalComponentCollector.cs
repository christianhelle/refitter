using Microsoft.OpenApi;

namespace Refitter.Core.Validation;

/// <summary>
/// Collects the components that a document reaches through external references. The walker does not follow
/// references, so the components of a referenced file are only checked and counted when they are collected
/// here and walked as part of a document of their own.
/// </summary>
internal sealed class ExternalComponentCollector : OpenApiVisitorBase
{
    public OpenApiComponents Components { get; } = new()
    {
        Schemas = new Dictionary<string, IOpenApiSchema>(),
        Responses = new Dictionary<string, IOpenApiResponse>(),
        Parameters = new Dictionary<string, IOpenApiParameter>(),
        Examples = new Dictionary<string, IOpenApiExample>(),
        RequestBodies = new Dictionary<string, IOpenApiRequestBody>(),
        Headers = new Dictionary<string, IOpenApiHeader>(),
        SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(),
        Links = new Dictionary<string, IOpenApiLink>(),
        Callbacks = new Dictionary<string, IOpenApiCallback>(),
        PathItems = new Dictionary<string, IOpenApiPathItem>(),
    };

    /// <summary>
    /// The number of components collected so far, to tell whether a walk found anything new.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// A copy of the components, which can be walked while the collector is still adding to the original.
    /// </summary>
    public OpenApiComponents Snapshot() => new()
    {
        Schemas = new Dictionary<string, IOpenApiSchema>(Components.Schemas!),
        Responses = new Dictionary<string, IOpenApiResponse>(Components.Responses!),
        Parameters = new Dictionary<string, IOpenApiParameter>(Components.Parameters!),
        Examples = new Dictionary<string, IOpenApiExample>(Components.Examples!),
        RequestBodies = new Dictionary<string, IOpenApiRequestBody>(Components.RequestBodies!),
        Headers = new Dictionary<string, IOpenApiHeader>(Components.Headers!),
        SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>(Components.SecuritySchemes!),
        Links = new Dictionary<string, IOpenApiLink>(Components.Links!),
        Callbacks = new Dictionary<string, IOpenApiCallback>(Components.Callbacks!),
        PathItems = new Dictionary<string, IOpenApiPathItem>(Components.PathItems!),
    };

    public override void Visit(IOpenApiReferenceHolder referenceHolder)
    {
        switch (referenceHolder)
        {
            case OpenApiSchemaReference { Reference.IsExternal: true } reference:
                Add(Components.Schemas!, reference.Reference, reference.Target);
                break;
            case OpenApiResponseReference { Reference.IsExternal: true } reference:
                Add(Components.Responses!, reference.Reference, reference.Target);
                break;
            case OpenApiParameterReference { Reference.IsExternal: true } reference:
                Add(Components.Parameters!, reference.Reference, reference.Target);
                break;
            case OpenApiExampleReference { Reference.IsExternal: true } reference:
                Add(Components.Examples!, reference.Reference, reference.Target);
                break;
            case OpenApiRequestBodyReference { Reference.IsExternal: true } reference:
                Add(Components.RequestBodies!, reference.Reference, reference.Target);
                break;
            case OpenApiHeaderReference { Reference.IsExternal: true } reference:
                Add(Components.Headers!, reference.Reference, reference.Target);
                break;
            case OpenApiSecuritySchemeReference { Reference.IsExternal: true } reference:
                Add(Components.SecuritySchemes!, reference.Reference, reference.Target);
                break;
            case OpenApiLinkReference { Reference.IsExternal: true } reference:
                Add(Components.Links!, reference.Reference, reference.Target);
                break;
            case OpenApiCallbackReference { Reference.IsExternal: true } reference:
                Add(Components.Callbacks!, reference.Reference, reference.Target);
                break;
            case OpenApiPathItemReference { Reference.IsExternal: true } reference:
                Add(Components.PathItems!, reference.Reference, reference.Target);
                break;
        }
    }

    private void Add<T>(IDictionary<string, T> components, BaseOpenApiReference reference, T? target)
        where T : class
    {
        // A reference whose file was not served has no target, and there is nothing to check
        if (target is null || reference.Id is null)
            return;

        // Two files can define a component with the same name, so the second one is keyed by its file too
        var key = reference.Id;
        if (components.TryGetValue(key, out var existing))
        {
            if (ReferenceEquals(existing, target))
                return;

            key = reference.ExternalResource + "#" + reference.Id;
            if (components.ContainsKey(key))
                return;
        }

        components[key] = target;
        Count++;
    }
}
