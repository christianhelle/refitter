namespace Refitter.Core;

/// <summary>Turns a discriminated oneOf/anyOf union into inheritance (the members derive from the union schema).</summary>
internal sealed class OneOfDiscriminatorToAllOfMutator : IDocumentMutator
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

            if (schema.Type is ApiObjectTypes.None or ApiObjectTypes.Null)
                schema.Type = ApiObjectTypes.Object;

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
