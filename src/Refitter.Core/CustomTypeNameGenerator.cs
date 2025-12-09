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
        // Check if we have a type override for this schema's type and format combination
        if (settings.TypeOverrides?.Length > 0 &&
            !string.IsNullOrEmpty(schema.Format))
        {
            // Convert JsonObjectType enum to lowercase string (e.g., JsonObjectType.String -> "string")
            var typeString = schema.Type.ToString().ToLowerInvariant();
            var formatPattern = $"{typeString}:{schema.Format}";
            var typeOverride = settings.TypeOverrides.FirstOrDefault(
                to => to.FormatPattern?.Equals(formatPattern, StringComparison.OrdinalIgnoreCase) == true);

            if (typeOverride != null && !string.IsNullOrEmpty(typeOverride.TypeName))
            {
                return typeOverride.TypeName;
            }
        }

        return defaultGenerator.Generate(schema, typeNameHint, reservedTypeNames);
    }
}
