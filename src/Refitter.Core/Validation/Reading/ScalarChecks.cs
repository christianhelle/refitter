using System.Globalization;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Parses scalar values only to fail where Microsoft.OpenApi (MIT license) fails: it parses these values into its
/// object model, so a malformed value ends validation with the parse exception.
/// </summary>
internal static class ScalarChecks
{
    public static void CheckBoolean(string value) => _ = bool.Parse(value);

    public static void CheckInt32(string value, IFormatProvider provider) => _ = int.Parse(value, provider);

    public static void CheckUInt32(string value, IFormatProvider provider) => _ = uint.Parse(value, provider);

    public static void CheckDecimal(string value, IFormatProvider provider) => _ = decimal.Parse(value, provider);

    public static void CheckDecimal(string value, NumberStyles style, IFormatProvider provider) =>
        _ = decimal.Parse(value, style, provider);
}
