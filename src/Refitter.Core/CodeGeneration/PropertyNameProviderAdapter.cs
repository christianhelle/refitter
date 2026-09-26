using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// Exposes an <see cref="IPropertyNameProvider"/> to NJsonSchema as an <see cref="IPropertyNameGenerator"/>.
/// </summary>
internal sealed class PropertyNameProviderAdapter(IPropertyNameProvider provider) : IPropertyNameGenerator
{
    internal IPropertyNameProvider Provider => provider;

    public string Generate(JsonSchemaProperty property) =>
        provider.GetPropertyName(new PropertyNameContext(property.Name, property.IsRequired));
}
