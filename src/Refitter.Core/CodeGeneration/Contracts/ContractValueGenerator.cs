using System.Globalization;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Generates C# expressions for schema default values and numeric bounds. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ContractValueGenerator(ContractGeneratorSettings settings)
{
    private static readonly Regex NumberRegex = new(@"^[0-9]+(\.[0-9]+)?$", RegexOptions.Compiled);

    private static readonly HashSet<string> UnsupportedFormatStrings = new(StringComparer.Ordinal)
    {
        "date", "date-time", "time", "duration", "time-span", "uri", "guid", "byte", "uuid", "base64",
    };

    private static readonly List<string> TypesWithStringConstructor = ["System.Guid", "System.Uri"];

    public string? GetDefaultValue(
        ApiSchema schema,
        bool allowsNull,
        string targetType,
        string? typeNameHint,
        bool useSchemaDefault,
        ContractTypeResolver typeResolver)
    {
        var defaultValue = GetSchemaDefaultValue(schema, typeNameHint, useSchemaDefault, typeResolver);
        if (defaultValue != null)
            return defaultValue;

        if (schema.Default != null && useSchemaDefault)
        {
            if (TypesWithStringConstructor.Contains(targetType))
                return "new " + targetType + "(" + GetDefaultAsStringLiteral(schema) + ")";

            if (targetType is "System.DateTime" or "System.DateTime?")
                return "System.DateTime.Parse(" + GetDefaultAsStringLiteral(schema) + ")";
        }

        var isOptionalProperty = schema is ApiSchemaProperty { IsRequired: false };
        var actualSchema = schema.ActualSchema;
        if (!allowsNull && !isOptionalProperty && (actualSchema.Type.IsArray() || actualSchema.Type.IsObject()))
        {
            if (!string.IsNullOrEmpty(settings.DictionaryInstanceType) &&
                targetType.StartsWith(settings.DictionaryType + "<", StringComparison.Ordinal))
            {
                targetType = settings.DictionaryInstanceType + targetType.Substring(settings.DictionaryType.Length);
            }

            if (!string.IsNullOrEmpty(settings.ArrayInstanceType) &&
                targetType.StartsWith(settings.ArrayType + "<", StringComparison.Ordinal))
            {
                targetType = settings.ArrayInstanceType + targetType.Substring(settings.ArrayType.Length);
            }

            return actualSchema.IsAbstract ? null : "new " + targetType + "()";
        }

        return null;
    }

    public string GetNumericValue(ApiObjectType type, object value, string? format)
    {
        switch (format)
        {
            case "byte":
                return "(byte)" + Convert.ToByte(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case "int32":
                return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case "int64":
                return Convert.ToInt64(value, CultureInfo.InvariantCulture) + "L";
            case "uint64":
                return Convert.ToUInt64(value, CultureInfo.InvariantCulture) + "UL";
            case "double":
                return ConvertNumberToString(value) + "D";
            case "float":
                return ConvertNumberToString(value) + "F";
            case "decimal":
                return ConvertNumberToString(value) + "M";
            default:
                return type.IsInteger() ? ConvertNumberToString(value) : ConvertNumberToString(value) + "D";
        }
    }

    public static string ConvertNumberToString(object value) =>
        value switch
        {
            byte b => b.ToString(CultureInfo.InvariantCulture),
            sbyte sb => sb.ToString(CultureInfo.InvariantCulture),
            short s => s.ToString(CultureInfo.InvariantCulture),
            ushort us => us.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            uint ui => ui.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            ulong ul => ul.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString("r", CultureInfo.InvariantCulture),
            double d => d.ToString("r", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            string text when NumberRegex.IsMatch(text) => text,
            _ => value.ToString()!,
        };

    private string? GetSchemaDefaultValue(
        ApiSchema schema,
        string? typeNameHint,
        bool useSchemaDefault,
        ContractTypeResolver typeResolver)
    {
        if (schema.Default == null || !useSchemaDefault)
            return null;

        var actualSchema = schema is ApiSchemaProperty property ? property.ActualTypeSchema : schema.ActualSchema;
        if (actualSchema.IsEnumeration && !actualSchema.Type.IsObject() && actualSchema.Type != ApiObjectType.None)
            return GetEnumDefaultValue(schema, actualSchema, typeNameHint, typeResolver);

        if (schema.Type.IsString() && (schema.Format == null || !UnsupportedFormatStrings.Contains(schema.Format)))
            return GetDefaultAsStringLiteral(schema);

        if (schema.Type.IsBoolean())
            return schema.Default.ToString()!.ToLowerInvariant();

        if (schema.Type.IsInteger() || schema.Type.IsNumber())
            return GetNumericValue(schema.Type, schema.Default, schema.Format);

        return null;
    }

    private string GetEnumDefaultValue(
        ApiSchema schema,
        ApiSchema actualSchema,
        string? typeNameHint,
        ContractTypeResolver typeResolver)
    {
        var typeName = typeResolver.Resolve(actualSchema, isNullable: false, typeNameHint);
        var index = actualSchema.Enumeration.IndexOf(schema.Default);
        var name = index >= 0 && actualSchema.EnumerationNames.Count > index
            ? actualSchema.EnumerationNames[index]
            : schema.Default?.ToString();

        return settings.Namespace + "." + typeName.Trim('?') + "." +
               settings.EnumNameGenerator.Generate(index, name, schema.Default, actualSchema);
    }

    private static string GetDefaultAsStringLiteral(ApiSchema schema) =>
        ConversionUtilities.ConvertToStringLiteral(schema.Default?.ToString() ?? string.Empty, "\"", "\"");
}
