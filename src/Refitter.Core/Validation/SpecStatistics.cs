using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation;

/// <summary>
/// Counts the elements of a <see cref="SpecDocument"/> the way the Microsoft.OpenApi (MIT license) walker visits
/// them: elements that are only a <c>$ref</c> are not counted, components are, and response and encoding header
/// counts include references.
/// </summary>
internal sealed class SpecStatistics
{
    private readonly OpenApiStats stats = new();
    private readonly Stack<SpecSchema> schemaLoop = new();
    private readonly Stack<SpecPathItem> pathItemLoop = new();

    public static OpenApiStats Count(SpecDocument? document)
    {
        var statistics = new SpecStatistics();
        if (document != null)
            statistics.Walk(document);

        return statistics.stats;
    }

    private void Walk(SpecDocument document)
    {
        if (document.Paths != null)
        {
            foreach (var pathItem in document.Paths.Values)
            {
                Walk(pathItem);
            }
        }

        WalkAll(document.Webhooks, Walk);

        var components = document.Components;
        if (components != null)
        {
            WalkAll(components.Schemas, Walk);
            WalkAll(components.Callbacks, Walk);
            WalkAll(components.PathItems, Walk);
            WalkAll(components.Parameters, Walk);
            WalkAll(components.Headers, Walk);
            WalkAll(components.Links, Walk);
            WalkAll(components.RequestBodies, Walk);
            WalkAll(components.Responses, Walk);
            WalkAll(components.MediaTypes, Walk);
        }
    }

    private void Walk(SpecPathItem? pathItem)
    {
        if (pathItem == null || pathItem.Reference != null || pathItemLoop.Contains(pathItem))
            return;

        pathItemLoop.Push(pathItem);
        stats.PathItemCount++;
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
        stats.OperationCount++;
        WalkAll(operation.Parameters, Walk);
        Walk(operation.RequestBody);
        if (operation.Responses != null)
        {
            stats.ResponseCount += operation.Responses.Count;
            WalkAll(operation.Responses, Walk);
        }

        WalkAll(operation.Callbacks, Walk);
    }

    private void Walk(SpecParameter? parameter)
    {
        if (parameter == null || parameter.Reference != null)
            return;

        stats.ParameterCount++;
        Walk(parameter.Schema);
        WalkAll(parameter.Content, Walk);
    }

    private void Walk(SpecRequestBody? requestBody)
    {
        if (requestBody == null || requestBody.Reference != null)
            return;

        stats.RequestBodyCount++;
        WalkAll(requestBody.Content, Walk);
    }

    private void Walk(SpecResponse? response)
    {
        if (response == null || response.Reference != null)
            return;

        WalkAll(response.Content, Walk);
        WalkAll(response.Links, Walk);
        WalkAll(response.Headers, Walk);
    }

    private void Walk(SpecMediaType? mediaType)
    {
        if (mediaType == null || mediaType.Reference != null)
            return;

        Walk(mediaType.Schema);
        if (mediaType.Encoding != null)
        {
            foreach (var encoding in mediaType.Encoding.Values)
            {
                if (encoding?.Headers == null)
                    continue;

                stats.HeaderCount += encoding.Headers.Count;
                WalkAll(encoding.Headers, Walk);
            }
        }
    }

    private void Walk(SpecHeader? header)
    {
        if (header == null || header.Reference != null)
            return;

        WalkAll(header.Content, Walk);
        Walk(header.Schema);
    }

    private void Walk(SpecLink? link)
    {
        if (link == null || link.Reference != null)
            return;

        stats.LinkCount++;
    }

    private void Walk(SpecCallback? callback)
    {
        if (callback == null || callback.Reference != null)
            return;

        stats.CallbackCount++;
        WalkAll(callback.PathItems, Walk);
    }

    private void Walk(SpecSchema? schema)
    {
        if (schema == null || schema.Reference != null || schemaLoop.Contains(schema))
            return;

        schemaLoop.Push(schema);
        stats.SchemaCount++;
        Walk(schema.Items);
        Walk(schema.Not);
        WalkAll(schema.AllOf, Walk);
        WalkAll(schema.AnyOf, Walk);
        WalkAll(schema.OneOf, Walk);
        WalkAll(schema.Properties, Walk);
        Walk(schema.AdditionalProperties);
        schemaLoop.Pop();
    }

    private static void WalkAll<T>(IEnumerable<T>? items, Action<T> walk)
    {
        if (items == null)
            return;

        foreach (var item in items)
        {
            walk(item);
        }
    }

    private static void WalkAll<T>(IDictionary<string, T>? items, Action<T> walk)
    {
        if (items == null)
            return;

        foreach (var item in items.Values)
        {
            walk(item);
        }
    }
}
