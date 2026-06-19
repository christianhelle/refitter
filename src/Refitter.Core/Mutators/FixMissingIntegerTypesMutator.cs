using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal sealed class FixMissingIntegerTypesMutator : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        SchemaWalker.TraverseDocumentSchemas(document, FixSchemaTypeFromFormat);
    }

    private static void FixSchemaTypeFromFormat(JsonSchema schema)
    {
        if ((schema.Type == JsonObjectType.None || schema.Type == JsonObjectType.Null) &&
            !string.IsNullOrEmpty(schema.Format))
        {
            if (schema.Format == "int32" || schema.Format == "int64")
            {
                schema.Type = JsonObjectType.Integer;
            }
            else if (schema.Format == "float" || schema.Format == "double")
            {
                schema.Type = JsonObjectType.Number;
            }
        }
    }
}
