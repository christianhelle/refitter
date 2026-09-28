using System.Globalization;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Recognizes ISO 8601 date-times in untyped JSON values (defaults, examples, enum values). Such values have
/// always been read as <see cref="DateTime"/> rather than as strings, which shows in the generated code
/// (e.g. string defaults), so they still are.
/// </summary>
internal static class IsoDateTimeParser
{
    private static readonly Regex IsoRegex = new(
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})T(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})(?:\.(?<fraction>\d{1,7}))?(?<zone>Z|z|[+-]\d{2}(?::?\d{2})?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex MicrosoftDateRegex = new(
        @"^/Date\((?<ticks>-?\d+)(?<offset>[+-]\d{4})?\)/$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static bool TryParse(string text, out DateTime value)
    {
        value = default;
        if (text.Length == 0)
            return false;

        if (text[0] == '/')
            return TryParseMicrosoftDate(text, out value);

        if (text.Length < 19 || text.Length > 40 || !char.IsDigit(text[0]) || text[10] != 'T')
            return false;

        var match = IsoRegex.Match(text);
        if (!match.Success)
            return false;

        var year = Parse(match, "year");
        var month = Parse(match, "month");
        var day = Parse(match, "day");
        var hour = Parse(match, "hour");
        var minute = Parse(match, "minute");
        var second = Parse(match, "second");

        if (month < 1 || month > 12 || year < 1 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return false;

        if (hour > 24 || minute >= 60 || second >= 60 || (hour == 24 && (minute != 0 || second != 0)))
            return false;

        var is24Hour = hour == 24;
        if (is24Hour)
            hour = 0;

        var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        var fraction = match.Groups["fraction"];
        if (fraction.Success)
        {
            var digits = fraction.Value.PadRight(7, '0');
            dateTime = dateTime.AddTicks(long.Parse(digits, CultureInfo.InvariantCulture));
        }

        if (is24Hour)
            dateTime = dateTime.AddDays(1);

        var zone = match.Groups["zone"];
        if (!zone.Success)
        {
            value = dateTime;
            return true;
        }

        if (zone.Value is "Z" or "z")
        {
            value = new DateTime(dateTime.Ticks, DateTimeKind.Utc);
            return true;
        }

        var sign = zone.Value[0] == '-' ? -1 : 1;
        var offsetText = zone.Value.Substring(1).Replace(":", string.Empty);
        var offsetHours = int.Parse(offsetText.Substring(0, 2), CultureInfo.InvariantCulture);
        var offsetMinutes = offsetText.Length > 2 ? int.Parse(offsetText.Substring(2, 2), CultureInfo.InvariantCulture) : 0;
        var offset = new TimeSpan(offsetHours, offsetMinutes, 0);
        var ticks = dateTime.Ticks - (sign * offset.Ticks);
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
        {
            ticks += dateTime.ToLocalTime().Ticks - dateTime.Ticks;
            value = new DateTime(Math.Min(Math.Max(ticks, DateTime.MinValue.Ticks), DateTime.MaxValue.Ticks), DateTimeKind.Local);
            return true;
        }

        value = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
        return true;
    }

    private static bool TryParseMicrosoftDate(string text, out DateTime value)
    {
        value = default;
        var match = MicrosoftDateRegex.Match(text);
        if (!match.Success ||
            !long.TryParse(match.Groups["ticks"].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return false;
        }

        var utc = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        value = match.Groups["offset"].Success ? utc.ToLocalTime() : utc;
        return true;
    }

    private static int Parse(Match match, string group) =>
        int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);
}
