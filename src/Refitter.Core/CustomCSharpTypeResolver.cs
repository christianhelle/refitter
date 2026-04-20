using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CustomCSharpTypeResolver : CSharpTypeResolver
{
    private readonly Dictionary<string, string>? formatMappings;
    private readonly CSharpGeneratorSettings settings;

    public CustomCSharpTypeResolver(
        CSharpGeneratorSettings settings,
        Dictionary<string, string>? formatMappings)
        : base(settings)
    {
        this.formatMappings = formatMappings;
        this.settings = settings;
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
            // Check if the mapped type is already nullable (value type wrapped in Nullable<>)
            if (mappedType.StartsWith("System.Nullable<", StringComparison.Ordinal) ||
                mappedType.StartsWith("Nullable<", StringComparison.Ordinal))
            {
                return mappedType;
            }

            // Only append ? if the type is a value type and nullable reference types are enabled
            // For reference types, only append ? if GenerateNullableReferenceTypes is true
            if (isNullable && !mappedType.EndsWith("?", StringComparison.Ordinal))
            {
                // Try to determine if this is a value type by checking for common patterns
                var isKnownValueType = IsValueType(mappedType);

                if (isKnownValueType)
                {
                    // Value types can always use ?
                    return $"{mappedType}?";
                }
                else if (settings.GenerateNullableReferenceTypes)
                {
                    // Reference types can only use ? when NRT is enabled
                    return $"{mappedType}?";
                }
            }

            return mappedType;
        }

        // Fall back to default NSwag type resolution
        return base.Resolve(schema, isNullable, typeNameHint);
    }

    private static readonly HashSet<string> KnownValueTypes
        = new(StringComparer.Ordinal)
        {
            // System namespace qualified names
            "System.Guid", "System.DateTime", "System.DateTimeOffset",
            "System.TimeSpan", "System.DateOnly", "System.TimeOnly", "System.Decimal", "System.Int32", "System.Int64",
            "System.Double", "System.Single", "System.Boolean", "System.Byte",
            "System.SByte", "System.Int16", "System.UInt16", "System.UInt32",
            "System.UInt64", "System.Char",
            // Unqualified names
            "Guid", "DateTime", "DateTimeOffset", "TimeSpan", "DateOnly", "TimeOnly", "Decimal",
            "Int32", "Int64", "Double", "Single", "Boolean", "Byte",
            "SByte", "Int16", "UInt16", "UInt32", "UInt64", "Char",
            // C# type aliases
            "bool", "byte", "sbyte", "char", "decimal", "double",
            "float", "int", "uint", "long", "ulong", "short", "ushort"
        };

    private static bool IsValueType(string typeName)
        => KnownValueTypes.Contains(typeName);
}
