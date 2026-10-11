using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema reads <c>x-enumDescriptions</c> and <c>x-enum-descriptions</c> as arrays parallel to <c>enum</c>,
/// so a document using the object form keyed by enum value (as docusaurus-theme-openapi-docs renders it) fails
/// to load. This converts that object form to the array form in a JSON document before NSwag parses it.
/// Other spellings are not bound by NJsonSchema, so they load as plain extension data and are left alone.
/// </summary>
internal static class EnumDescriptionsSanitizer
{
    private static readonly HashSet<string> EnumDescriptionsKeywords = new(StringComparer.Ordinal)
    {
        "x-enumDescriptions",
        "x-enum-descriptions",
    };

    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized.
    private static readonly Regex CandidateRegex = new(
        @"""x-enum(?:Descriptions|-descriptions)""\s*:\s*\{",
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

        if (JToken.ReadFrom(reader) is not JContainer document)
            return json;

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            if (!EnumDescriptionsKeywords.Contains(property.Name) || property.Value is not JObject descriptions)
                continue;

            if (property.Parent?["enum"] is JArray enumValues)
            {
                // Keep the array parallel to enum: values without a description get an empty one
                property.Value = new JArray(enumValues.Select(value => descriptions[GetKey(value)] ?? string.Empty));
            }
            else
            {
                // Nothing to line the descriptions up with, and leaving the object would fail the load
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
