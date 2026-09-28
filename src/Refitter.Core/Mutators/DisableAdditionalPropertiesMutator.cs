namespace Refitter.Core;

/// <summary>Disallows additional properties on all named schemas (when default additional properties are off).</summary>
internal sealed class DisableAdditionalPropertiesMutator(bool generateDefaultAdditionalProperties) : IDocumentMutator
{
    public void Mutate(ApiDocument document)
    {
        if (generateDefaultAdditionalProperties)
            return;

        foreach (var schema in document.Components.Schemas.Values)
        {
            schema.ActualSchema.AllowAdditionalProperties = false;
        }
    }
}
