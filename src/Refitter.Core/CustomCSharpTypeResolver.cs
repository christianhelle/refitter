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
        var format = schema.Format;

        // Check if this schema has a format with a custom mapping
        if (formatMappings != null &&
            format is { Length: > 0 } &&
            formatMappings.TryGetValue(format, out var mappedType))
        {
            // Return the custom mapped type with nullability
            return isNullable && !mappedType.EndsWith("?") && !mappedType.Contains("Nullable<")
                ? $"{mappedType}?"
                : mappedType;
        }

        // Fall back to default NSwag type resolution
        return base.Resolve(schema, isNullable, typeNameHint);
    }
}
