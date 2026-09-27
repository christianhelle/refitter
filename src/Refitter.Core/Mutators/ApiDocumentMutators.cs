namespace Refitter.Core;

/// <summary>Changes a document before code is generated from it.</summary>
internal interface IApiDocumentMutator
{
    void Mutate(ApiDocument document);
}

/// <summary>Visits every schema of a document once.</summary>
internal static class ApiSchemaWalker
{
    public static void TraverseDocumentSchemas(ApiDocument document, Action<ApiSchema> visitor)
    {
        var visited = new HashSet<ApiSchema>();
        var schemasToProcess = new Stack<ApiSchema>();

        foreach (var schema in EnumerateDocumentSchemaRoots(document))
        {
            if (schema != null)
                schemasToProcess.Push(schema);
        }

        while (schemasToProcess.Count > 0)
        {
            var actualSchema = schemasToProcess.Pop().ActualSchema;
            if (!visited.Add(actualSchema))
                continue;

            visitor(actualSchema);

            foreach (var childSchema in EnumerateTraversableSchemas(actualSchema))
            {
                if (childSchema != null)
                    schemasToProcess.Push(childSchema);
            }
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

                foreach (var parameter in operation.ActualParameters)
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

/// <summary>Disallows additional properties on all named schemas (when default additional properties are off).</summary>
internal sealed class DisableAdditionalPropertiesApiMutator(bool generateDefaultAdditionalProperties) : IApiDocumentMutator
{
    public void Mutate(ApiDocument document)
    {
        if (generateDefaultAdditionalProperties)
            return;

        foreach (var schema in document.Components.Schemas.Values)
        {
            schema.ActualSchema.AllowAdditionalProperties = false;
        }
    }
}

/// <summary>
/// Collapses a schema whose <c>allOf</c> contains a single primitive sub-schema (e.g. a <c>$ref</c> wrapped to add a
/// description) into that primitive type, so no class is generated that derives from a primitive type.
/// </summary>
internal sealed class FlattenPrimitiveAllOfApiMutator : IApiDocumentMutator
{
    public void Mutate(ApiDocument document) => ApiSchemaWalker.TraverseDocumentSchemas(document, Flatten);

    private static void Flatten(ApiSchema schema)
    {
        if (schema.AllOf.Count != 1)
            return;

        if (schema.Type == ApiObjectType.Object ||
            schema.Properties.Count != 0 ||
            schema.OneOf.Count != 0 ||
            schema.AnyOf.Count != 0)
        {
            return;
        }

        var inner = schema.AllOf.First().ActualSchema;
        if (!IsPrimitive(inner.Type))
            return;

        schema.Type = inner.Type;
        schema.Format = inner.Format;

        if (string.IsNullOrEmpty(schema.Description))
            schema.Description = inner.Description;

        if (inner.IsEnumeration)
        {
            foreach (var value in inner.Enumeration)
                schema.Enumeration.Add(value);

            foreach (var name in inner.EnumerationNames)
                schema.EnumerationNames.Add(name);
        }

        schema.AllOf.Clear();
    }

    private static bool IsPrimitive(ApiObjectType type) =>
        type is ApiObjectType.String or ApiObjectType.Integer or ApiObjectType.Number or ApiObjectType.Boolean;
}

/// <summary>Turns a discriminated oneOf/anyOf union into inheritance (the members derive from the union schema).</summary>
internal sealed class OneOfDiscriminatorToAllOfApiMutator : IApiDocumentMutator
{
    public void Mutate(ApiDocument document)
    {
        foreach (var definition in document.Components.Schemas)
        {
            var schema = definition.Value?.ActualSchema;
            if (schema?.DiscriminatorObject == null)
                continue;

            var unionSchemas = schema.OneOf.Concat(schema.AnyOf).ToArray();
            if (unionSchemas.Length == 0)
                continue;

            if (schema.Type is ApiObjectType.None or ApiObjectType.Null)
                schema.Type = ApiObjectType.Object;

            foreach (var subSchemaReference in unionSchemas)
            {
                var subSchema = subSchemaReference?.ActualSchema;
                if (subSchema == null)
                    continue;

                var alreadyInherits = subSchema.AllOf.Any(a => a.HasReference && a.ActualSchema == schema);
                if (!alreadyInherits)
                {
                    subSchema.AllOf.Add(new ApiSchema { Reference = schema });
                }
            }

            schema.OneOf.Clear();
            schema.AnyOf.Clear();
        }
    }
}

/// <summary>Gives schemas with an integer or number format but no type that type.</summary>
internal sealed class FixMissingIntegerTypesApiMutator : IApiDocumentMutator
{
    public void Mutate(ApiDocument document) => ApiSchemaWalker.TraverseDocumentSchemas(document, Fix);

    private static void Fix(ApiSchema schema)
    {
        if ((schema.Type == ApiObjectType.None || schema.Type == ApiObjectType.Null) && !string.IsNullOrEmpty(schema.Format))
        {
            if (schema.Format is "int32" or "int64")
            {
                schema.Type = ApiObjectType.Integer;
            }
            else if (schema.Format is "float" or "double")
            {
                schema.Type = ApiObjectType.Number;
            }
        }
    }
}

/// <summary>Uses 64-bit integers for integers without a format (when the integer type is Int64).</summary>
internal sealed class CustomIntegerTypeApiMutator(IntegerType customIntegerType) : IApiDocumentMutator
{
    public void Mutate(ApiDocument document)
    {
        if (customIntegerType == IntegerType.Int32)
            return;

        ApiSchemaWalker.TraverseDocumentSchemas(document, schema =>
        {
            if (schema.Type == ApiObjectType.Integer && string.IsNullOrEmpty(schema.Format))
                schema.Format = "int64";
        });
    }
}
