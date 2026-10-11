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

    public static string Sanitize(string json)
    {
        if (JToken.Parse(json) is not JContainer document)
            return json;

        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            if (property.Name == EnumDescriptionsKeyword &&
                property.Value is JObject descriptions &&
                property.Parent?["enum"] is JArray enumValues)
            {
                property.Value = new JArray(enumValues.Select(value => descriptions[(string)value!]));
            }
        }

        return document.ToString(Formatting.None);
    }
}
