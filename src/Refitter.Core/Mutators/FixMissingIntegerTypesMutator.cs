namespace Refitter.Core;

/// <summary>Gives schemas with an integer or number format but no type that type.</summary>
internal sealed class FixMissingIntegerTypesMutator : IDocumentMutator
{
    public void Mutate(ApiDocument document) => SchemaWalker.TraverseDocumentSchemas(document, Fix);

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
