namespace Refitter.Core.Validation.Model;

/// <summary>
/// The kind of component a <c>$ref</c> points to.
/// </summary>
internal enum SpecComponentType
{
    Schema,
    Response,
    Parameter,
    Example,
    RequestBody,
    Header,
    SecurityScheme,
    Link,
    Callback,
    PathItem,
    MediaType,
}

/// <summary>
/// A <c>$ref</c> found in a document, which can be pointed somewhere else.
/// </summary>
internal sealed class SpecReferenceSite(SpecComponentType type, SpecReference reference, Action<SpecReference> update)
{
    public SpecComponentType Type { get; } = type;

    public SpecReference Reference { get; } = reference;

    public void Update(SpecReference reference) => update(reference);
}

/// <summary>
/// Visits every <c>$ref</c> of a document in the order the Microsoft.OpenApi (MIT license) walker visits them.
/// </summary>
internal sealed class SpecReferenceWalker
{
    private readonly Action<SpecReferenceSite> visit;
    private readonly Stack<SpecSchema> schemaLoop = new();
    private readonly Stack<SpecPathItem> pathItemLoop = new();

    private SpecReferenceWalker(Action<SpecReferenceSite> visit)
    {
        this.visit = visit;
    }

    public static void Walk(SpecDocument document, Action<SpecReferenceSite> visit) =>
        new SpecReferenceWalker(visit).WalkDocument(document);

    private void WalkDocument(SpecDocument document)
    {
        foreach (var pathItem in document.Paths.Values)
        {
            Walk(pathItem);
        }

        WalkAll(document.Webhooks, Walk);

        var components = document.Components;
        if (components != null)
        {
            WalkAll(components.Schemas, Walk);
            WalkAll(components.SecuritySchemes, Walk);
            WalkAll(components.Callbacks, Walk);
            WalkAll(components.PathItems, Walk);
            WalkAll(components.Parameters, Walk);
            WalkAll(components.Examples, Walk);
            WalkAll(components.Headers, Walk);
            WalkAll(components.Links, Walk);
            WalkAll(components.RequestBodies, Walk);
            WalkAll(components.Responses, Walk);
            WalkAll(components.MediaTypes, Walk);
        }

        WalkAll(document.Security, Walk);
    }

    private bool IsReference(SpecReferenceable element, SpecComponentType type)
    {
        if (element.Reference == null)
            return false;

        visit(new SpecReferenceSite(type, element.Reference, reference => element.Reference = reference));
        return true;
    }

    private void Walk(SpecPathItem? pathItem)
    {
        if (pathItem == null || IsReference(pathItem, SpecComponentType.PathItem) || pathItemLoop.Contains(pathItem))
            return;

        pathItemLoop.Push(pathItem);
        WalkAll(pathItem.Parameters, Walk);
        if (pathItem.Operations != null)
        {
            foreach (var operation in pathItem.Operations.Values)
            {
                Walk(operation);
            }
        }

        pathItemLoop.Pop();
    }

    private void Walk(SpecOperation operation)
    {
        WalkAll(operation.Parameters, Walk);
        Walk(operation.RequestBody);
        WalkAll(operation.Responses, Walk);
        WalkAll(operation.Callbacks, Walk);
        WalkAll(operation.Security, Walk);
    }

    private void Walk(SpecSecurityRequirement requirement)
    {
        for (var i = 0; i < requirement.Count; i++)
        {
            var index = i;
            visit(new SpecReferenceSite(SpecComponentType.SecurityScheme, requirement[index], reference => requirement[index] = reference));
        }
    }

    private void Walk(SpecParameter? parameter)
    {
        if (parameter == null || IsReference(parameter, SpecComponentType.Parameter))
            return;

        Walk(parameter.Schema);
        WalkAll(parameter.Content, Walk);
        WalkAll(parameter.Examples, Walk);
    }

    private void Walk(SpecRequestBody? requestBody)
    {
        if (requestBody == null || IsReference(requestBody, SpecComponentType.RequestBody))
            return;

        WalkAll(requestBody.Content, Walk);
    }

    private void Walk(SpecResponse? response)
    {
        if (response == null || IsReference(response, SpecComponentType.Response))
            return;

        WalkAll(response.Content, Walk);
        WalkAll(response.Links, Walk);
        WalkAll(response.Headers, Walk);
    }

    private void Walk(SpecMediaType? mediaType)
    {
        if (mediaType == null || IsReference(mediaType, SpecComponentType.MediaType))
            return;

        WalkAll(mediaType.Examples, Walk);
        Walk(mediaType.Schema);
        if (mediaType.Encoding != null)
        {
            foreach (var encoding in mediaType.Encoding.Values)
            {
                WalkAll(encoding?.Headers, Walk);
            }
        }
    }

    private void Walk(SpecHeader? header)
    {
        if (header == null || IsReference(header, SpecComponentType.Header))
            return;

        WalkAll(header.Content, Walk);
        WalkAll(header.Examples, Walk);
        Walk(header.Schema);
    }

    private void Walk(SpecLink? link)
    {
        if (link != null)
            IsReference(link, SpecComponentType.Link);
    }

    private void Walk(SpecCallback? callback)
    {
        if (callback == null || IsReference(callback, SpecComponentType.Callback))
            return;

        WalkAll(callback.PathItems, Walk);
    }

    private void Walk(SpecExample? example)
    {
        if (example != null)
            IsReference(example, SpecComponentType.Example);
    }

    private void Walk(SpecSecurityScheme? securityScheme)
    {
        if (securityScheme != null)
            IsReference(securityScheme, SpecComponentType.SecurityScheme);
    }

    private void Walk(SpecSchema? schema)
    {
        if (schema == null || IsReference(schema, SpecComponentType.Schema) || schemaLoop.Contains(schema))
            return;

        schemaLoop.Push(schema);
        Walk(schema.Items);
        Walk(schema.Not);
        WalkAll(schema.AllOf, Walk);
        WalkAll(schema.AnyOf, Walk);
        WalkAll(schema.OneOf, Walk);
        WalkAll(schema.Properties, Walk);
        Walk(schema.AdditionalProperties);
        if (schema.Discriminator?.Mapping != null)
        {
            foreach (var mapping in schema.Discriminator.Mapping.Values)
            {
                Walk(mapping);
            }
        }

        schemaLoop.Pop();
    }

    private static void WalkAll<T>(IEnumerable<T>? items, Action<T> walk)
    {
        if (items == null)
            return;

        foreach (var item in items.ToList())
        {
            walk(item);
        }
    }

    private static void WalkAll<T>(IDictionary<string, T>? items, Action<T> walk)
    {
        if (items == null)
            return;

        foreach (var item in items.Values.ToList())
        {
            walk(item);
        }
    }
}
