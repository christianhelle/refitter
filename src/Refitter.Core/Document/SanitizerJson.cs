using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// Reads and rewrites JSON documents for <see cref="OpenApiJsonNormalizer"/>.
/// </summary>
internal static class SanitizerJson
{
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
    /// Parses a JSON document. Numbers are read as decimals so that re-serializing keeps them exact. Only a
    /// document with a number outside the decimal range, like an out-of-range bound, falls back to doubles.
    /// </summary>
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
