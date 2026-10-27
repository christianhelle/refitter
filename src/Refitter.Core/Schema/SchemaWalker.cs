using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal static class SchemaWalker
{
    public static void TraverseDocumentSchemas(
        OpenApiDocument document,
        Action<JsonSchema> visitor)
    {
        var visited = new HashSet<JsonSchema>(JsonSchemaReferenceComparer.Instance);
        var schemasToProcess = new Stack<JsonSchema>();

        foreach (var schema in EnumerateDocumentSchemaRoots(document))
        {
            TryPush(schema, schemasToProcess);
        }

        while (schemasToProcess.Count > 0)
        {
            var actualSchema = schemasToProcess.Pop().ActualSchema;
            if (!visited.Add(actualSchema))
            {
                continue;
            }

            visitor(actualSchema);

            foreach (var childSchema in EnumerateTraversableSchemas(actualSchema))
            {
                TryPush(childSchema, schemasToProcess);
            }
        }
    }

    private static IEnumerable<JsonSchema?> EnumerateDocumentSchemaRoots(
        OpenApiDocument document)
    {
        foreach (var schema in document.Components.Schemas.Values)
        {
            yield return schema;
        }

        foreach (var pathItem in document.Paths.Values.Where(pathItem => pathItem != null))
        {
            foreach (var schema in EnumeratePathItemSchemas(pathItem))
            {
                yield return schema;
            }
        }
    }

    private static IEnumerable<JsonSchema?> EnumeratePathItemSchemas(OpenApiPathItem pathItem)
    {
        foreach (var parameter in pathItem.Parameters)
        {
            yield return parameter;
        }

        foreach (var operation in pathItem.Values.Where(operation => operation != null))
        {
            foreach (var schema in EnumerateOperationSchemas(operation))
            {
                yield return schema;
            }
        }
    }

    private static IEnumerable<JsonSchema?> EnumerateOperationSchemas(OpenApiOperation operation)
    {
        foreach (var parameter in operation.ActualParameters)
        {
            yield return parameter;
        }

        if (operation.RequestBody?.Content != null)
        {
            foreach (var content in operation.RequestBody.Content.Values)
            {
                yield return content.Schema;
            }
        }

        foreach (var response in operation.ActualResponses.Values)
        {
            foreach (var schema in EnumerateResponseSchemas(response))
            {
                yield return schema;
            }
        }
    }

    private static IEnumerable<JsonSchema?> EnumerateResponseSchemas(OpenApiResponse response)
    {
        foreach (var header in response.Headers.Values)
        {
            yield return header;
        }

        foreach (var content in response.Content.Values)
        {
            yield return content.Schema;
        }
    }

    private static IEnumerable<JsonSchema?> EnumerateTraversableSchemas(JsonSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.DictionaryKey;
        yield return schema.Item;

        if (schema.Items.Count != 0)
        {
            foreach (var item in schema.Items)
            {
                yield return item;
            }
        }

        yield return schema.Not;

        foreach (var property in schema.Properties.Values)
        {
            yield return property;
        }

        foreach (var subSchema in schema.AllOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.OneOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.AnyOf)
        {
            yield return subSchema;
        }

        foreach (var definition in schema.Definitions.Values)
        {
            yield return definition;
        }
    }

    private static void TryPush(JsonSchema? schema, Stack<JsonSchema> stack)
    {
        if (schema == null)
        {
            return;
        }

        stack.Push(schema);
    }
}
