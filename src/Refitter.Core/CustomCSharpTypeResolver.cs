using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CustomCSharpTypeResolver : CSharpTypeResolver
{
    private readonly Dictionary<string, string>? formatMappings;

    public CustomCSharpTypeResolver(
        CSharpGeneratorSettings settings,
        Dictionary<string, string>? formatMappings)
        : base(settings)
    {
        this.formatMappings = formatMappings;
    }

    public override string Resolve(
        JsonSchema schema,
        bool isNullable,
        string? typeNameHint)
    {
        // Check if this schema has a format with a custom mapping
        if (formatMappings != null &&
            !string.IsNullOrEmpty(schema.Format) &&
            formatMappings.TryGetValue(schema.Format, out var mappedType))
        {
            // Return the custom mapped type with nullability
            return isNullable && !mappedType.EndsWith("?") && !mappedType.Contains("<")
                ? $"{mappedType}?"
                : mappedType;
        }

        // Fall back to default NSwag type resolution
        return base.Resolve(schema, isNullable, typeNameHint);
    }
}
