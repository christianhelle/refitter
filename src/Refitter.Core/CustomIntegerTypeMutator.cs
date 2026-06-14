using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal sealed class CustomIntegerTypeMutator(IntegerType customIntegerType)
    : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        if (customIntegerType == IntegerType.Int32)
            return;

        SchemaWalker.TraverseDocumentSchemas(document, FixSchemaIntegerFormat);
    }

    private static void FixSchemaIntegerFormat(JsonSchema schema)
    {
        if (schema.Type == JsonObjectType.Integer &&
            string.IsNullOrEmpty(schema.Format))
        {
            schema.Format = "int64";
        }
    }
}
