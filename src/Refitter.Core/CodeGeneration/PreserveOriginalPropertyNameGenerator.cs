using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

internal class PreserveOriginalPropertyNameGenerator : IPropertyNameGenerator
{
    public string Generate(JsonSchemaProperty property) =>
        IdentifierUtils.ToCompilableIdentifier(property.Name);
}
