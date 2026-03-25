using System.Globalization;
using System.Text;

namespace Refitter.Core;

internal static class IdentifierUtils
{
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while"
    };

    /// <summary>
    /// Returns <c>{value}{counter}{suffix}</c> if <c>{value}{name}</c> exists in <paramref name="knownIdentifiers"/>
    /// else returns <c>{value}{name}</c>.
    /// </summary>
    public static string Counted(ISet<string> knownIdentifiers, string name, string suffix = "", string parent = "")
    {
        if (!knownIdentifiers.Contains(string.IsNullOrEmpty(parent) ? $"{name}{suffix}" : $"{parent}.{name}{suffix}"))
        {
            return $"{name}{suffix}";
        }

        var counter = 2;
        while (knownIdentifiers.Contains(string.IsNullOrEmpty(parent)
                   ? $"{name}{counter}{suffix}"
                   : $"{parent}.{name}{counter}{suffix}"))
            counter++;

        return $"{name}{counter}{suffix}";
    }

    private static readonly char[] IllegalSymbols =
    [
        ' ', '-', '.',
        '!', '@',
        '"', '\'',
        '\n', '\t',
        '#', '$', '%', '^', '&', '*', '+',
        ',', ':', ';',
        '(', ')', '[', ']', '}', '{',
        '|', '/', '\\'
    ];

    /// <summary>
    /// Removes invalid character from an identifier string
    /// </summary>
    public static string Sanitize(this string value)
    {
        const char dash = '-';

        if (string.IsNullOrEmpty(value))
            return value;

        // @ can be used and still make valid method names, but this should make most use cases safe.
        if (
            (value.First() < 'A' || value.First() > 'Z') &&
            (value.First() < 'a' || value.First() > 'z') &&
            value.First() != '_'
            )
        {
            value = "_" + value;
        }
        return string.Join(string.Empty, value.Split(IllegalSymbols, StringSplitOptions.RemoveEmptyEntries))
                .Trim(dash);
    }

    /// <summary>
    /// Converts an arbitrary value into a compilable C# identifier while preserving as much of the original shape as
    /// possible.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A compilable C# identifier.</returns>
    public static string ToCompilableIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "_";
        }

        var originalValue = value!;

        if (IsValidIdentifier(originalValue))
        {
            return originalValue.StartsWith("@", StringComparison.Ordinal)
                ? originalValue
                : EscapeReservedKeyword(originalValue);
        }

        var builder = new StringBuilder(originalValue.Length);
        var previousCharacterWasUnderscore = false;

        foreach (var character in originalValue)
        {
            if (builder.Length == 0 && character == '@' && originalValue.Length > 1)
            {
                continue;
            }

            if (IsValidIdentifierCharacter(character, builder.Length == 0))
            {
                builder.Append(character);
                previousCharacterWasUnderscore = false;
                continue;
            }

            if (builder.Length == 0)
            {
                builder.Append('_');
                previousCharacterWasUnderscore = true;

                if (IsValidIdentifierCharacter(character, false))
                {
                    builder.Append(character);
                    previousCharacterWasUnderscore = false;
                }

                continue;
            }

            if (!previousCharacterWasUnderscore)
            {
                builder.Append('_');
                previousCharacterWasUnderscore = true;
            }
        }

        var identifier = builder.Length == 0 ? "_" : builder.ToString();

        if (!IsValidIdentifierCharacter(identifier[0], isFirstCharacter: true))
        {
            identifier = "_" + identifier;
        }

        return EscapeReservedKeyword(identifier);
    }

    /// <summary>
    /// Determines whether the supplied value is already a valid C# identifier.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns><c>true</c> when the value is already a valid identifier; otherwise, <c>false</c>.</returns>
    public static bool IsValidIdentifier(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var identifier = value!;

        if (identifier[0] == '@')
        {
            var unescapedIdentifier = identifier.Substring(1);

            return identifier.Length > 1 &&
                   !IsReservedKeyword(unescapedIdentifier) &&
                   IsValidIdentifierCore(unescapedIdentifier);
        }

        return IsValidIdentifierCore(identifier);
    }

    /// <summary>
    /// Escapes reserved C# keywords with the verbatim identifier prefix.
    /// </summary>
    /// <param name="value">The identifier to escape.</param>
    /// <returns>The escaped identifier.</returns>
    public static string EscapeReservedKeyword(string value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith("@", StringComparison.Ordinal))
        {
            return value;
        }

        return IsReservedKeyword(value)
            ? "@" + value
            : value;
    }

    /// <summary>
    /// Sanitizes and formats controller tags for identifier usage.
    /// </summary>
    /// <param name="tag">The tag to sanitize.</param>
    /// <returns>A sanitized, title-cased identifier string.</returns>
    public static string SanitizeControllerTag(this string tag)
    {
        return tag.Sanitize().CapitalizeFirstCharacter();
    }

    private static bool IsValidIdentifierCore(string value)
    {
        if (!IsValidIdentifierCharacter(value[0], isFirstCharacter: true))
        {
            return false;
        }

        for (var index = 1; index < value.Length; index++)
        {
            if (!IsValidIdentifierCharacter(value[index], isFirstCharacter: false))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsReservedKeyword(string value) =>
        ReservedKeywords.Contains(value);

    private static bool IsValidIdentifierCharacter(char value, bool isFirstCharacter)
    {
        if (value == '_')
        {
            return true;
        }

        var category = char.GetUnicodeCategory(value);

        return isFirstCharacter
            ? category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.LetterNumber
            : category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.LetterNumber
                or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.Format;
    }
}
