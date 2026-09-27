namespace Refitter.Core;

/// <summary>Uses 64-bit integers for integers without a format (when the integer type is Int64).</summary>
internal sealed class CustomIntegerTypeMutator(IntegerType customIntegerType) : IDocumentMutator
{
    public void Mutate(ApiDocument document)
    {
        if (customIntegerType == IntegerType.Int32)
            return;

        SchemaWalker.TraverseDocumentSchemas(document, schema =>
        {
            if (schema.Type == ApiObjectType.Integer && string.IsNullOrEmpty(schema.Format))
                schema.Format = "int64";
        });
    }
}
