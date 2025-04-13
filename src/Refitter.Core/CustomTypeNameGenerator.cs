using NJsonSchema;

namespace Refitter.Core;

public class CustomTypeNameGenerator(
    CodeGeneratorSettings settings)
    : ITypeNameGenerator
{
    private readonly ITypeNameGenerator defaultGenerator
        = new DefaultTypeNameGenerator();

    public string Generate(
        JsonSchema schema,
        string? typeNameHint,
        IEnumerable<string> reservedTypeNames)
    {
        // TODO: 
        // Implement custom logic to generate type names based on the schema and settings.
        // This is a placeholder implementation that simply uses the default generator.

        return defaultGenerator.Generate(schema, typeNameHint, reservedTypeNames);
    }
}
