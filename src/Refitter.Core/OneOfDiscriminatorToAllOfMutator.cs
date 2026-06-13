using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal sealed class OneOfDiscriminatorToAllOfMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        if (document.Components?.Schemas == null)
            return;

        foreach (var kvp in document.Components.Schemas)
        {
            var schema = kvp.Value?.ActualSchema;
            if (schema == null)
                continue;

            if (schema.DiscriminatorObject == null)
                continue;

            var unionSchemas = schema.OneOf.Concat(schema.AnyOf).ToArray();
            if (unionSchemas.Length == 0)
                continue;

            if (schema.Type == JsonObjectType.None || schema.Type == JsonObjectType.Null)
                schema.Type = JsonObjectType.Object;

            foreach (var subSchemaRef in unionSchemas)
            {
                var subSchema = subSchemaRef?.ActualSchema;
                if (subSchema == null)
                    continue;

                bool alreadyInherits = subSchema.AllOf.Any(
                    a => a.HasReference && a.ActualSchema == schema);
                if (!alreadyInherits)
                {
                    var reference = new JsonSchema { Reference = schema };
                    subSchema.AllOf.Add(reference);
                }
            }

            schema.OneOf.Clear();
            schema.AnyOf.Clear();
        }
    }
}
