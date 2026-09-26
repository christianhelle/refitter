using System.Globalization;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// NJsonSchema stores numeric bounds as <see cref="decimal"/>, so a document with a bound outside the
/// decimal range (e.g. <c>maximum: 1.7976931348623157E+308</c> emitted for <c>double.MaxValue</c>) fails to load.
/// This clamps such bounds to the decimal range in the raw JSON or YAML text before NSwag parses it.
/// </summary>
internal static class NumericBoundsSanitizer
{
    private static readonly string MaxValue = decimal.MaxValue.ToString(CultureInfo.InvariantCulture);
    private static readonly string MinValue = decimal.MinValue.ToString(CultureInfo.InvariantCulture);

    private static readonly Regex BoundRegex = new(
        """(?<=^|[\s{,])(?<prefix>(?<quote>["']?)(?:minimum|maximum|exclusiveMinimum|exclusiveMaximum)\k<quote>\s*:\s*)(?<number>-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)(?=\s*(?:[,}\]#\r\n]|$))""",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(5));

    public static string Sanitize(string content)
    {
        return BoundRegex.Replace(content, match =>
        {
            var number = match.Groups["number"].Value;
            if (IsWithinDecimalRange(number))
                return match.Value;

            var clamped = number.StartsWith("-", StringComparison.Ordinal) ? MinValue : MaxValue;
            return match.Groups["prefix"].Value + clamped;
        });
    }

    private static bool IsWithinDecimalRange(string number) =>
        double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
        Math.Abs(value) < (double)decimal.MaxValue;
}
