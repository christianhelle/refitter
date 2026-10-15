using NSwag;

namespace Refitter.Core;

internal sealed class ForceAdditionalPropertiesMutator(bool forceAdditionalProperties)
    : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        if (!forceAdditionalProperties)
            return;

        foreach (var kvp in document.Components.Schemas)
        {
            var schema = kvp.Value.ActualSchema;
            if (schema.AdditionalPropertiesSchema is null)
                schema.AllowAdditionalProperties = true;
        }
    }
}
