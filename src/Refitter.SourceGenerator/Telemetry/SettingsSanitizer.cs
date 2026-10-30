using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>
/// Reduces a .refitter file to which settings are used, so that telemetry never carries
/// paths, URLs, namespaces, comments or other free text that could be sensitive.
/// Only known setting names are reported. Booleans, numbers and defined enum members are kept;
/// every other value is redacted.
/// </summary>
internal static class SettingsSanitizer
{
    internal const string Redacted = "<redacted>";

    internal const string UnknownSettingsKey = "unknown-settings";

    private static readonly Regex JsonNumber = new(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Returns a flat JSON object of setting paths and sanitized values, or null if the file can't be read as JSON.
    /// Only setting names from <see cref="KnownSettings"/> are reported; other keys are only counted.
    /// </summary>
    public static string? Sanitize(string contents)
    {
        object? root;
        try
        {
            root = new Reader(contents).ReadDocument();
        }
        catch (FormatException)
        {
            return null;
        }

        if (root is not List<KeyValuePair<string, object?>> settings)
        {
            return null;
        }

        var values = new List<KeyValuePair<string, string>>();
        var unknownSettings = Flatten(settings, string.Empty, values);
        if (unknownSettings > 0)
        {
            values.Add(new KeyValuePair<string, string>(UnknownSettingsKey, unknownSettings.ToString(CultureInfo.InvariantCulture)));
        }

        return "{" + string.Join(",", values.Select(pair => Quote(pair.Key) + ":" + pair.Value)) + "}";
    }

    /// <summary>Adds the known settings to <paramref name="values"/> and returns how many unknown settings were skipped.</summary>
    private static int Flatten(
        List<KeyValuePair<string, object?>> settings,
        string prefix,
        List<KeyValuePair<string, string>> values)
    {
        var unknownSettings = 0;
        foreach (var setting in settings)
        {
            var nestedName = prefix.Length == 0 ? FindKnown(KnownSettings.NestedSettings, setting.Key) : null;
            if (setting.Value is List<KeyValuePair<string, object?>> nested && nestedName is not null)
            {
                unknownSettings += Flatten(nested, nestedName + ".", values);
                continue;
            }

            var path = FindKnown(KnownSettings.Paths.Keys, prefix + setting.Key);
            if (path is null)
            {
                unknownSettings++;
                continue;
            }

            values.Add(new KeyValuePair<string, string>(path, SanitizeValue(KnownSettings.Paths[path], setting.Value)));
        }

        return unknownSettings;
    }

    // Settings are read case-insensitively, but always reported with their canonical name
    private static string? FindKnown(IEnumerable<string> known, string name) =>
        known.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));

    private static string SanitizeValue(string[]? enumMembers, object? value)
    {
        switch (value)
        {
            case Literal literal:
                return literal.Text;
            case string text when enumMembers is not null:
                return Quote(FindKnown(enumMembers, text) ?? Redacted);
            case List<object?> array:
                return Quote($"[{array.Count.ToString(CultureInfo.InvariantCulture)} items]");
            default:
                return Quote(Redacted);
        }
    }

    private static string Quote(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < ' ')
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

        return builder.Append('"').ToString();
    }

    /// <summary>A JSON true, false, null or number, kept as written.</summary>
    private sealed class Literal(string text)
    {
        public string Text { get; } = text;
    }

    /// <summary>
    /// Minimal JSON reader that also accepts the comments and trailing commas allowed in .refitter files.
    /// </summary>
    private sealed class Reader(string text)
    {
        private int position;

        public object? ReadDocument()
        {
            var value = ReadValue();
            SkipWhitespaceAndComments();
            if (position != text.Length)
            {
                throw new FormatException("Unexpected content after JSON document");
            }

            return value;
        }

        private object? ReadValue()
        {
            SkipWhitespaceAndComments();
            if (position >= text.Length)
            {
                throw new FormatException("Unexpected end of JSON");
            }

            switch (text[position])
            {
                case '{':
                    return ReadObject();
                case '[':
                    return ReadArray();
                case '"':
                    return ReadString();
                default:
                    return ReadLiteral();
            }
        }

        private List<KeyValuePair<string, object?>> ReadObject()
        {
            var result = new List<KeyValuePair<string, object?>>();
            position++;
            while (true)
            {
                SkipWhitespaceAndComments();
                if (Peek() == '}')
                {
                    position++;
                    return result;
                }

                var key = ReadString();
                SkipWhitespaceAndComments();
                Expect(':');
                result.Add(new KeyValuePair<string, object?>(key, ReadValue()));
                if (!ReadSeparator('}'))
                {
                    return result;
                }
            }
        }

        private List<object?> ReadArray()
        {
            var result = new List<object?>();
            position++;
            while (true)
            {
                SkipWhitespaceAndComments();
                if (Peek() == ']')
                {
                    position++;
                    return result;
                }

                result.Add(ReadValue());
                if (!ReadSeparator(']'))
                {
                    return result;
                }
            }
        }

        /// <summary>Reads a comma (returns true) or the closing character (returns false).</summary>
        private bool ReadSeparator(char closing)
        {
            SkipWhitespaceAndComments();
            var c = Peek();
            position++;
            if (c == ',')
            {
                return true;
            }

            if (c == closing)
            {
                return false;
            }

            throw new FormatException($"Expected ',' or '{closing}'");
        }

        private string ReadString()
        {
            Expect('"');
            var builder = new StringBuilder();
            while (true)
            {
                var c = Peek();
                position++;
                if (c == '"')
                {
                    return builder.ToString();
                }

                builder.Append(c == '\\' ? ReadEscape() : c);
            }
        }

        /// <summary>Reads the escape sequence after a backslash, rejecting escapes that JSON doesn't allow.</summary>
        private char ReadEscape()
        {
            var escaped = Peek();
            position++;
            if (escaped != 'u')
            {
                return escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'b' => '\b',
                    'f' => '\f',
                    '"' or '\\' or '/' => escaped,
                    _ => throw new FormatException($"Invalid escape sequence '\\{escaped}'"),
                };
            }

            if (position + 4 > text.Length)
            {
                throw new FormatException("Invalid unicode escape");
            }

            var unicode = (char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            position += 4;
            return unicode;
        }

        private static bool IsJsonNumber(string literal)
        {
            try
            {
                return JsonNumber.IsMatch(literal);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private Literal ReadLiteral()
        {
            var start = position;
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] is '-' or '+' or '.'))
            {
                position++;
            }

            var literal = text.Substring(start, position - start);
            if (literal is "true" or "false" or "null" || IsJsonNumber(literal))
            {
                return new Literal(literal);
            }

            throw new FormatException($"Unexpected token '{literal}'");
        }

        private void SkipWhitespaceAndComments()
        {
            while (position < text.Length)
            {
                if (char.IsWhiteSpace(text[position]))
                {
                    position++;
                }
                else if (StartsWith("//"))
                {
                    var end = text.IndexOf('\n', position);
                    position = end < 0 ? text.Length : end + 1;
                }
                else if (StartsWith("/*"))
                {
                    var end = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                    position = end < 0 ? text.Length : end + 2;
                }
                else
                {
                    return;
                }
            }
        }

        private bool StartsWith(string value) =>
            string.CompareOrdinal(text, position, value, 0, value.Length) == 0;

        private char Peek() =>
            position < text.Length ? text[position] : throw new FormatException("Unexpected end of JSON");

        private void Expect(char expected)
        {
            if (Peek() != expected)
            {
                throw new FormatException($"Expected '{expected}'");
            }

            position++;
        }
    }
}
