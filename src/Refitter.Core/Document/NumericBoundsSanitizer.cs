using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema stores numeric bounds as <see cref="decimal"/>, so a document with a bound outside the
/// decimal range (e.g. <c>maximum: 1.7976931348623157E+308</c> emitted for <c>double.MaxValue</c>) fails to load.
/// This clamps such bounds to the decimal range in a JSON document before NSwag parses it.
/// </summary>
internal static class NumericBoundsSanitizer
{
    private static readonly HashSet<string> BoundKeywords = new(StringComparer.Ordinal)
    {
        "minimum",
        "maximum",
        "exclusiveMinimum",
        "exclusiveMaximum",
    };

    // A cheap text scan that finds candidate bounds, so that documents without out-of-range
    // bounds are returned untouched instead of being parsed and re-serialized.
    private static readonly Regex CandidateBoundRegex = new(
        """["']?(?:minimum|maximum|exclusiveMinimum|exclusiveMaximum)["']?\s*:\s*["']?(?<number>-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)""",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string json)
    {
        var hasCandidate = CandidateBoundRegex
            .Matches(json)
            .Cast<Match>()
            .Any(match => !IsWithinDecimalRange(match.Groups["number"].Value));

        if (!hasCandidate)
            return json;

        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double,
        };

        if (JToken.ReadFrom(reader) is not JContainer document)
            return json;

        var clamped = false;
        foreach (var property in document.Descendants().OfType<JProperty>().ToList())
        {
            if (BoundKeywords.Contains(property.Name) &&
                property.Value is JValue value &&
                TryClamp(value, out var clampedValue))
            {
                // Written as a string, like every scalar in documents NSwag converts from YAML: Newtonsoft
                // parses strings to decimal exactly, while a number literal can be read back as a double that
                // rounds above decimal.MaxValue. Assigning in place also avoids JProperty comparing the old
                // double with the new value, which overflows.
                value.Value = clampedValue.ToString(CultureInfo.InvariantCulture);
                clamped = true;
            }
        }

        return clamped ? document.ToString(Formatting.None) : json;
    }

    private static bool TryClamp(JValue value, out decimal clampedValue)
    {
        clampedValue = 0;
        var text = Convert.ToString(value.Value, CultureInfo.InvariantCulture);
        if (text is null ||
            IsWithinDecimalRange(text) ||
            !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        clampedValue = number < 0 ? decimal.MinValue : decimal.MaxValue;
        return true;
    }

    private static bool IsWithinDecimalRange(string number) =>
        decimal.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
}
