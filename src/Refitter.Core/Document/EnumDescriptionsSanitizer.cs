using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema reads <c>x-enumDescriptions</c> as an array parallel to <c>enum</c>, so a document using the
/// object form keyed by enum value (as docusaurus-theme-openapi-docs renders it) fails to load.
/// This converts that object form to the array form in a JSON document before NSwag parses it.
/// </summary>
internal static class EnumDescriptionsSanitizer
{
    private const string EnumDescriptionsKeyword = "x-enumDescriptions";

    // A cheap text scan that finds candidate objects, so that documents without them
    // are returned untouched instead of being parsed and re-serialized.
    private static readonly Regex CandidateRegex = new(
        @"""x-enumDescriptions""\s*:\s*\{",
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
            if (property.Name != EnumDescriptionsKeyword || property.Value is not JObject descriptions)
                continue;

            if (property.Parent?["enum"] is JArray enumValues)
            {
                // Keep the array parallel to enum: values without a description get an empty one
                property.Value = new JArray(enumValues.Select(value => descriptions[(string)value!] ?? string.Empty));
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
}
