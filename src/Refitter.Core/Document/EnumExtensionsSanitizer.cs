using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema reads the enum description extensions (<c>x-enumDescriptions</c>, <c>x-enum-descriptions</c>) and
/// the enum name extensions (<c>x-enumNames</c>, <c>x-enum-names</c>, <c>x-enum-varnames</c>) as arrays parallel
/// to <c>enum</c>, so a document using the object form keyed by enum value (as docusaurus-theme-openapi-docs
/// renders descriptions) fails to load. This converts that object form to the array form in a JSON document
/// before NSwag parses it. Other spellings are not bound by NJsonSchema, so they load as plain extension data
/// and are left alone.
/// </summary>
internal static class EnumExtensionsSanitizer
{
    private static readonly HashSet<string> DescriptionKeywords = new(StringComparer.Ordinal)
    {
        "x-enumDescriptions",
        "x-enum-descriptions",
    };

    private static readonly HashSet<string> NameKeywords = new(StringComparer.Ordinal)
    {
        "x-enumNames",
        "x-enum-names",
        "x-enum-varnames",
    };

    // OpenAPI and JSON Schema maps whose keys are user-chosen names
    private static readonly HashSet<string> NamedMapKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "dependentSchemas",
        "definitions",
        "$defs",
        "schemas",
        "responses",
        "parameters",
        "examples",
        "requestBodies",
        "headers",
        "securitySchemes",
        "securityDefinitions",
        "links",
        "callbacks",
        "pathItems",
        "webhooks",
        "content",
        "encoding",
        "variables",
    };

    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized. A \u escape could spell
    // a property name the scan cannot see, so documents with one are always parsed.
    private static readonly Regex CandidateRegex = new(
        @"""x-enum(?:Descriptions|-descriptions|Names|-names|-varnames)""\s*:\s*\{|\\u",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string json)
    {
        if (!CandidateRegex.IsMatch(json))
            return json;

        var document = Parse(json);

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            var isName = NameKeywords.Contains(property.Name);
            if ((!isName && !DescriptionKeywords.Contains(property.Name)) || property.Value is not JObject valuesByKey)
                continue;

            // An entry of a named map (a schema, response, parameter, ...) that happens to be named like the extension
            if (property.Parent!.Parent is JProperty { Name: var mapName } && NamedMapKeywords.Contains(mapName))
                continue;

            if (property.Parent["enum"] is JArray enumValues)
            {
                // Keep the array parallel to enum. Values without a description get an empty one, and values
                // without a name get the enum value, so the member is named as if no names were given
                property.Value = new JArray(enumValues.Select(value =>
                    FindEntry(valuesByKey, value) ?? (isName ? GetKey(value) : string.Empty)));
            }
            else
            {
                // Nothing to line the entries up with, and leaving the object would fail the load
                property.Remove();
            }

            converted = true;
        }

        return converted ? document.ToString(Formatting.None) : json;
    }

    // Numbers are read as decimals so that re-serializing keeps them exact. Only a document with a number
    // outside the decimal range falls back to doubles, the way NumericBoundsSanitizer reads documents.
    private static JContainer Parse(string json)
    {
        try
        {
            return Parse(json, FloatParseHandling.Decimal);
        }
        catch (JsonReaderException)
        {
            return Parse(json, FloatParseHandling.Double);
        }
    }

    private static JContainer Parse(string json, FloatParseHandling floatParseHandling)
    {
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = floatParseHandling,
        };

        // The candidate scan only matches a property name, so the root is an object or array
        return (JContainer)JToken.ReadFrom(reader);
    }

    private static JToken? FindEntry(JObject valuesByKey, JToken value)
    {
        var entry = valuesByKey[GetKey(value)];

        // Only numbers have JSON text that parses as a number; strings are quoted, and true or null are not
        if (entry is not null || !TryParseDecimal(value.ToString(Formatting.None), out var number))
            return entry;

        // Keys can spell a number differently from the enum value, like 1e2 for 100 or 2.50 for 2.5
        return valuesByKey.Properties()
            .FirstOrDefault(key => TryParseDecimal(key.Name, out var keyNumber) && keyNumber == number)
            ?.Value;
    }

    private static bool TryParseDecimal(string text, out decimal number) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    // Object keys are always strings, so other enum values (1, true, null) are matched by their JSON text
    private static string GetKey(JToken value) =>
        value.Type == JTokenType.String ? (string)value! : value.ToString(Formatting.None);
}
