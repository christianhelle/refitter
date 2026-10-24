using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Reads JSON documents for the sanitizers that rewrite them before NSwag parses them.
/// </summary>
internal static class SanitizerJson
{
    /// <summary>
    /// Offers every property of the document to <paramref name="tryConvert"/>, which rewrites it in place and
    /// returns whether it did. The candidate scan is a cheap text check, so documents it does not match are
    /// returned untouched instead of being parsed and re-serialized.
    /// </summary>
    public static string Rewrite(string json, Regex candidateRegex, Func<JProperty, bool> tryConvert)
    {
        if (!candidateRegex.IsMatch(json))
            return json;

        var document = Parse(json);

        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
            converted |= tryConvert(property);

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

        // The sanitizers' candidate scans only match a property name, so the root is an object or array
        return (JContainer)JToken.ReadFrom(reader);
    }
}
