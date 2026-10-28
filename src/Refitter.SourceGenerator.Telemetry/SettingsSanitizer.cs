using System.Globalization;
using System.Text;

namespace Refitter.SourceGenerator.Telemetry;

/// <summary>
/// Reduces a .refitter file to which settings are used, so that telemetry never carries
/// paths, URLs, namespaces, comments or other free text that could be sensitive.
/// Booleans, numbers and known enum values are kept; every other value is redacted.
/// </summary>
internal static class SettingsSanitizer
{
    internal const string Redacted = "<redacted>";

    private static readonly HashSet<string> NestedSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        "naming",
        "dependencyInjectionSettings",
        "apizrSettings",
        "codeGeneratorSettings",
    };

    private static readonly HashSet<string> EnumSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        "authenticationHeaderStyle",
        "collectionFormat",
        "integerType",
        "multipleInterfaces",
        "operationNameGenerator",
        "propertyNamingPolicy",
        "transientErrorHandler",
        "typeAccessibility",
        "withCacheProvider",
        "withMappingProvider",
    };

    /// <summary>
    /// Returns a flat JSON object of setting paths and sanitized values, or null if the file can't be read as JSON.
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
        Flatten(settings, string.Empty, values);
        return "{" + string.Join(",", values.Select(pair => TelemetryPayload.Quote(pair.Key) + ":" + pair.Value)) + "}";
    }

    private static void Flatten(
        List<KeyValuePair<string, object?>> settings,
        string prefix,
        List<KeyValuePair<string, string>> values)
    {
        foreach (var setting in settings)
        {
            var path = prefix + setting.Key;
            if (setting.Value is List<KeyValuePair<string, object?>> nested && prefix.Length == 0 && NestedSettings.Contains(setting.Key))
            {
                Flatten(nested, path + ".", values);
                continue;
            }

            values.Add(new KeyValuePair<string, string>(path, SanitizeValue(setting.Key, setting.Value)));
        }
    }

    private static string SanitizeValue(string key, object? value) =>
        value switch
        {
            Literal literal => literal.Text,
            string text when EnumSettings.Contains(key) && text.Length <= 64 && text.All(char.IsLetterOrDigit) => TelemetryPayload.Quote(text),
            List<object?> array => TelemetryPayload.Quote($"[{array.Count.ToString(CultureInfo.InvariantCulture)} items]"),
            _ => TelemetryPayload.Quote(Redacted),
        };

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

                if (c == '\\')
                {
                    var escaped = Peek();
                    position++;
                    if (escaped == 'u')
                    {
                        if (position + 4 > text.Length)
                        {
                            throw new FormatException("Invalid unicode escape");
                        }

                        builder.Append((char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        position += 4;
                        continue;
                    }

                    builder.Append(escaped switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'b' => '\b',
                        'f' => '\f',
                        _ => escaped,
                    });
                    continue;
                }

                builder.Append(c);
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
            if (literal is "true" or "false" or "null" ||
                double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
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
