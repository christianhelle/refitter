#nullable enable

using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Refitter.Core;

/// <summary>
/// A JSON object kept as-is (e.g. an object default value or extension data). Its string form matches the
/// indented JSON Refitter has always written for such values.
/// </summary>
internal sealed class RawJsonObject
{
    public List<KeyValuePair<string, object?>> Properties { get; } = new();

    public bool TryGetValue(string name, out object? value)
    {
        // Duplicate keys: the last one wins, like a JSON object deserialized into a dictionary
        for (var i = Properties.Count - 1; i >= 0; i--)
        {
            if (Properties[i].Key == name)
            {
                value = Properties[i].Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    public override string ToString() => RawJson.ToIndentedString(this);
}

/// <summary>A JSON array kept as-is.</summary>
internal sealed class RawJsonArray
{
    public List<object?> Items { get; } = new();

    public override string ToString() => RawJson.ToIndentedString(this);
}

/// <summary>Converts JSON elements to the CLR values schemas hold for untyped JSON (defaults, enums, examples).</summary>
internal static class RawJson
{
    public static object? FromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new RawJsonObject();
                foreach (var property in element.EnumerateObject())
                {
                    obj.Properties.Add(new(property.Name, FromElement(property.Value)));
                }

                return obj;
            case JsonValueKind.Array:
                var array = new RawJsonArray();
                foreach (var item in element.EnumerateArray())
                {
                    array.Items.Add(FromElement(item));
                }

                return array;
            case JsonValueKind.String:
                var text = element.GetString()!;
                return IsoDateTimeParser.TryParse(text, out var dateTime) ? dateTime : text;
            case JsonValueKind.Number:
                return ParseNumber(element.GetRawText());
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }

    /// <summary>Integers become <see cref="long"/> (or <see cref="BigInteger"/>), other numbers <see cref="double"/>.</summary>
    public static object ParseNumber(string text)
    {
        var isInteger = text.IndexOfAny(['.', 'e', 'E']) < 0;
        if (isInteger)
        {
            if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var longValue))
                return longValue;

            if (BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var bigValue))
                return bigValue;
        }

        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public static string ToIndentedString(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value, 0);
        return builder.ToString();
    }

    private static void WriteObject(StringBuilder builder, RawJsonObject obj, int depth)
    {
        if (obj.Properties.Count == 0)
        {
            builder.Append("{}");
            return;
        }

        builder.Append('{');
        for (var i = 0; i < obj.Properties.Count; i++)
        {
            builder.Append(Environment.NewLine).Append(' ', (depth + 1) * 2);
            WriteString(builder, obj.Properties[i].Key);
            builder.Append(": ");
            Write(builder, obj.Properties[i].Value, depth + 1);
            if (i < obj.Properties.Count - 1)
                builder.Append(',');
        }

        builder.Append(Environment.NewLine).Append(' ', depth * 2).Append('}');
    }

    private static void WriteArray(StringBuilder builder, RawJsonArray array, int depth)
    {
        if (array.Items.Count == 0)
        {
            builder.Append("[]");
            return;
        }

        builder.Append('[');
        for (var i = 0; i < array.Items.Count; i++)
        {
            builder.Append(Environment.NewLine).Append(' ', (depth + 1) * 2);
            Write(builder, array.Items[i], depth + 1);
            if (i < array.Items.Count - 1)
                builder.Append(',');
        }

        builder.Append(Environment.NewLine).Append(' ', depth * 2).Append(']');
    }

    private static void Write(StringBuilder builder, object? value, int depth)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case RawJsonObject obj:
                WriteObject(builder, obj, depth);
                break;
            case RawJsonArray array:
                WriteArray(builder, array, depth);
                break;
            case string text:
                WriteString(builder, text);
                break;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                break;
            case DateTime dateTime:
                WriteString(builder, dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture));
                break;
            case double number:
                builder.Append(FormatDouble(number));
                break;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                break;
            default:
                builder.Append(value);
                break;
        }
    }

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsPositiveInfinity(value))
            return "Infinity";
        if (double.IsNegativeInfinity(value))
            return "-Infinity";

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        return text.IndexOfAny(['.', 'E', 'e']) >= 0 ? text : text + ".0";
    }

    private static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (var c in text)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (c < ' ' || c == (char)0x85 || c == (char)0x2028 || c == (char)0x2029)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}
