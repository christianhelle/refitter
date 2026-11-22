using System.Globalization;
using System.Text.RegularExpressions;
using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema writes numeric and boolean defaults and integer enum values into the generated code as they are,
/// so a JSON string in one of those positions would be emitted as C# code (GHSA-h924-jfhp-9vjr).
/// This drops numeric and boolean defaults that are not a number or a boolean, and generates integer enums
/// with a value that is not a number as string enums, whose values are written as escaped string literals.
/// </summary>
internal sealed class NonLiteralValueMutator : IOpenApiDocumentMutator
{
    private static readonly Regex NumberLiteral = new(
        @"^-?[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex WholeNumber = new(
        @"^-?[0-9]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public void Mutate(OpenApiDocument document)
    {
        SchemaWalker.TraverseDocumentSchemas(document, Sanitize);
    }

    private static void Sanitize(JsonSchema schema)
    {
        if (schema.Default is { } defaultValue && !IsLiteral(schema, defaultValue))
        {
            schema.Default = null;
        }

        if (schema.Type.HasFlag(JsonObjectType.Integer) &&
            schema.Enumeration.Any(value => value is not null && !IsNumber(value)))
        {
            schema.Type = (schema.Type & ~JsonObjectType.Integer) | JsonObjectType.String;
        }
    }

    // String defaults are escaped, but NJsonSchema skips the string branch for some formats (date-time, uuid, ...),
    // so a schema that is also boolean or numeric only keeps a default that is safe in those branches
    private static bool IsLiteral(JsonSchema schema, object value)
    {
        var type = schema.Type;
        var isBoolean = type.HasFlag(JsonObjectType.Boolean);
        var isNumeric = type.HasFlag(JsonObjectType.Integer) || type.HasFlag(JsonObjectType.Number);
        if (!isBoolean && !isNumeric)
            return true;

        if (isNumeric && !isBoolean && !type.HasFlag(JsonObjectType.Number) && IsNumber(value))
            return FitsInteger(schema, value);

        return (isBoolean && IsBoolean(value)) || (isNumeric && IsNumber(value));
    }

    // NJsonSchema writes an integer default as it is, so one that does not fit the CLR type of the property
    // (int, or long for int64) is a compile error
    private static bool FitsInteger(JsonSchema schema, object value)
    {
        string text = string.Format(CultureInfo.InvariantCulture, "{0}", value);
        if (!WholeNumber.IsMatch(text))
            return true;

        return schema.Format switch
        {
            "int64" => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _),
            "uint64" => ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _),
            "uint32" => uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _),
            _ => int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _),
        };
    }

    private static bool IsBoolean(object value) => value switch
    {
        bool => true,
        string text => text.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       text.Equals("false", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static bool IsNumber(object value) =>
        value is not bool &&
        NumberLiteral.IsMatch(string.Format(CultureInfo.InvariantCulture, "{0}", value));
}
