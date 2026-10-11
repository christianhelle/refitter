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

    private static readonly HashSet<string> SchemaMapKeywords = new(StringComparer.Ordinal)
    {
        "properties",
        "patternProperties",
        "definitions",
        "schemas",
        "$defs",
    };

    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized.
    private static readonly Regex CandidateRegex = new(
        @"""x-enum(?:Descriptions|-descriptions|Names|-names|-varnames)""\s*:\s*\{",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string json)
    {
        if (!CandidateRegex.IsMatch(json))
            return json;

        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
        };

        // The candidate scan only matches a property name, so the root is an object or array
        var document = (JContainer)JToken.ReadFrom(reader);

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            var isName = NameKeywords.Contains(property.Name);
            if ((!isName && !DescriptionKeywords.Contains(property.Name)) || property.Value is not JObject valuesByKey)
                continue;

            // Keys of these maps are user-chosen names, so this is a schema that happens to be named like the extension
            if (property.Parent!.Parent is JProperty { Name: var mapName } && SchemaMapKeywords.Contains(mapName))
                continue;

            if (property.Parent["enum"] is JArray enumValues)
            {
                // Keep the array parallel to enum. Values without a description get an empty one, and values
                // without a name get the enum value, so the member is named as if no names were given
                property.Value = new JArray(enumValues.Select(value =>
                    valuesByKey[GetKey(value)] ?? (isName ? GetKey(value) : string.Empty)));
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

    // Object keys are always strings, so other enum values (1, true, null) are matched by their JSON text
    private static string GetKey(JToken value) =>
        value.Type == JTokenType.String ? (string)value! : value.ToString(Formatting.None);
}
