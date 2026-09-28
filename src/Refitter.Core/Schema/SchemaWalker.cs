namespace Refitter.Core;

/// <summary>Visits every schema of a document once.</summary>
internal static class SchemaWalker
{
    public static void TraverseDocumentSchemas(ApiDocument document, Action<ApiSchema> visitor)
    {
        var visited = new HashSet<ApiSchema>();
        var schemasToProcess = new Stack<ApiSchema>();

        foreach (var schema in EnumerateDocumentSchemaRoots(document).OfType<ApiSchema>())
            schemasToProcess.Push(schema);

        while (schemasToProcess.Count > 0)
        {
            var actualSchema = schemasToProcess.Pop().ActualSchema;
            if (!visited.Add(actualSchema))
                continue;

            visitor(actualSchema);

            foreach (var childSchema in EnumerateTraversableSchemas(actualSchema).OfType<ApiSchema>())
                schemasToProcess.Push(childSchema);
        }
    }

    private static IEnumerable<ApiSchema?> EnumerateDocumentSchemaRoots(ApiDocument document)
    {
        foreach (var schema in document.Components.Schemas.Values)
            yield return schema;

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem == null)
                continue;

            foreach (var parameter in pathItem.Parameters)
                yield return parameter;

            foreach (var operation in pathItem.Values)
            {
                if (operation == null)
                    continue;

                foreach (var parameter in operation.GetActualParameters())
                    yield return parameter;

                if (operation.RequestBody?.Content != null)
                {
                    foreach (var content in operation.RequestBody.Content.Values)
                        yield return content.Schema;
                }

                foreach (var response in operation.ActualResponses.Values)
                {
                    foreach (var header in response.Headers.Values)
                        yield return header;

                    foreach (var content in response.Content.Values)
                        yield return content.Schema;
                }
            }
        }
    }

    private static IEnumerable<ApiSchema?> EnumerateTraversableSchemas(ApiSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.DictionaryKey;
        yield return schema.Item;

        foreach (var item in schema.Items)
            yield return item;

        yield return schema.Not;

        foreach (var property in schema.Properties.Values)
            yield return property;

        foreach (var subSchema in schema.AllOf)
            yield return subSchema;

        foreach (var subSchema in schema.OneOf)
            yield return subSchema;

        foreach (var subSchema in schema.AnyOf)
            yield return subSchema;

        foreach (var definition in schema.Definitions.Values)
            yield return definition;
    }
}
