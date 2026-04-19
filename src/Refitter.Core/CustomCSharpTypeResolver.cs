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
            if (isNullable && !mappedType.EndsWith("?"))
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

    private static bool IsValueType(string typeName)
    {
        // Common value types
        return typeName is "System.Guid" or "System.DateTime" or "System.DateTimeOffset"
            or "System.TimeSpan" or "System.Decimal" or "System.Int32" or "System.Int64"
            or "System.Double" or "System.Single" or "System.Boolean" or "System.Byte"
            or "System.SByte" or "System.Int16" or "System.UInt16" or "System.UInt32"
            or "System.UInt64" or "System.Char"
            // Also check for unqualified names
            or "Guid" or "DateTime" or "DateTimeOffset" or "TimeSpan" or "Decimal"
            or "Int32" or "Int64" or "Double" or "Single" or "Boolean" or "Byte"
            or "SByte" or "Int16" or "UInt16" or "UInt32" or "UInt64" or "Char"
            // C# aliases
            or "bool" or "byte" or "sbyte" or "char" or "decimal" or "double"
            or "float" or "int" or "uint" or "long" or "ulong" or "short"
            or "ushort";
    }
}
