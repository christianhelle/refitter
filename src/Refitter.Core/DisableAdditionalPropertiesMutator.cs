using NSwag;

namespace Refitter.Core;

internal sealed class DisableAdditionalPropertiesMutator(bool generateDefaultAdditionalProperties)
    : IOpenApiDocumentMutator
{
    public void Mutate(OpenApiDocument document)
    {
        if (generateDefaultAdditionalProperties)
            return;

        if (document.Components?.Schemas == null)
            return;

        foreach (var kvp in document.Components.Schemas)
        {
            kvp.Value.ActualSchema.AllowAdditionalProperties = false;
        }
    }
}
