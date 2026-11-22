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

    public void Mutate(OpenApiDocument document)
    {
        SchemaWalker.TraverseDocumentSchemas(document, Sanitize);
    }

    private static void Sanitize(JsonSchema schema)
    {
        if (schema.Default is { } defaultValue &&
            (!IsLiteral(schema.Type, defaultValue) || !IsEnumMember(schema, defaultValue)))
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
    private static bool IsLiteral(JsonObjectType type, object value)
    {
        var isBoolean = type.HasFlag(JsonObjectType.Boolean);
        var isNumeric = type.HasFlag(JsonObjectType.Integer) || type.HasFlag(JsonObjectType.Number);
        if (!isBoolean && !isNumeric)
            return true;

        return (isBoolean && IsBoolean(value)) || (isNumeric && IsNumber(value));
    }

    // NJsonSchema looks the default up among the enum values to build its member name, which throws for a default
    // that is not one of them. It uses Enumeration.IndexOf, which compares by equality, so membership is decided
    // the same way: a string "1" prints like the number 1 but is not a member of [1, 2]
    private static bool IsEnumMember(JsonSchema schema, object value) =>
        schema.Enumeration.Count == 0 ||
        schema.Enumeration.Contains(value);

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
