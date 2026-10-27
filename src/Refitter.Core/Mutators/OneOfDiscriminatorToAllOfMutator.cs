using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal sealed class OneOfDiscriminatorToAllOfMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        foreach (var kvp in document.Components.Schemas)
        {
            var schema = kvp.Value?.ActualSchema;
            if (schema?.DiscriminatorObject != null)
                ConvertUnionToInheritance(schema);
        }
    }

    private static void ConvertUnionToInheritance(JsonSchema schema)
    {
        var unionSchemas = schema.OneOf.Concat(schema.AnyOf).ToArray();
        if (unionSchemas.Length == 0)
            return;

        if (schema.Type == JsonObjectType.None || schema.Type == JsonObjectType.Null)
            schema.Type = JsonObjectType.Object;

        foreach (var subSchema in unionSchemas.Select(subSchemaRef => subSchemaRef?.ActualSchema))
        {
            if (subSchema != null)
                InheritFrom(subSchema, schema);
        }

        schema.OneOf.Clear();
        schema.AnyOf.Clear();
    }

    private static void InheritFrom(JsonSchema subSchema, JsonSchema schema)
    {
        bool alreadyInherits = subSchema.AllOf.Any(
            a => a.HasReference && a.ActualSchema == schema);
        if (!alreadyInherits)
        {
            var reference = new JsonSchema { Reference = schema };
            subSchema.AllOf.Add(reference);
        }
    }
}
