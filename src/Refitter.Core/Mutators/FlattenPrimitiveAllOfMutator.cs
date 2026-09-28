namespace Refitter.Core;

/// <summary>
/// Collapses a schema whose <c>allOf</c> contains a single primitive sub-schema (e.g. a <c>$ref</c> wrapped to add a
/// description) into that primitive type, so no class is generated that derives from a primitive type.
/// </summary>
internal sealed class FlattenPrimitiveAllOfMutator : IDocumentMutator
{
    public void Mutate(ApiDocument document) => SchemaWalker.TraverseDocumentSchemas(document, Flatten);

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

        var inner = schema.AllOf[0].ActualSchema;
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
