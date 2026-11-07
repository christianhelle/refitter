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

        if (Parse(json) is not JContainer document)
            return json;

        return RewriteProperties(document, tryConvert) ? document.ToString(Formatting.None) : json;
    }

    /// <summary>
    /// Offers every property of <paramref name="document"/> to <paramref name="tryConvert"/>, which rewrites it in
    /// place, and returns whether any property was rewritten.
    /// </summary>
    public static bool RewriteProperties(JContainer document, Func<JProperty, bool> tryConvert)
    {
        var converted = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
            converted |= tryConvert(property);

        return converted;
    }

    /// <summary>
    /// Parses a JSON document.
    /// </summary>
    // Numbers are read as decimals so that re-serializing keeps them exact. Only a document with a number
    // outside the decimal range falls back to doubles, the way NumericBoundsSanitizer reads documents.
    public static JToken Parse(string json)
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

    private static JToken Parse(string json, FloatParseHandling floatParseHandling)
    {
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = floatParseHandling,
        };

        return JToken.ReadFrom(reader);
    }
}
