using System.Text;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>Generates C# type names for schemas. See THIRD-PARTY-NOTICES.md.</summary>
internal interface ISchemaTypeNameGenerator
{
    string Generate(ApiSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames);
}

/// <summary>
/// Derives a type name from a hint (usually the schema name) or the schema title, and adds a numeric suffix
/// when the name is already taken. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal class SchemaTypeNameGenerator : ISchemaTypeNameGenerator
{
    private static readonly char[] TypeNameHintCleanupChars = ['[', ']', '<', '>', ',', ' '];
    private static readonly Regex NonWordCharacterRegex = new(@"\W", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex RepeatedUnderscoreRegex = new("[_]{2,}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private readonly string[] reservedTypeNames = ["object"];

    public virtual string Generate(ApiSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames)
    {
        if (string.IsNullOrEmpty(typeNameHint) && !string.IsNullOrEmpty(schema.DocumentPath))
        {
            var segments = schema.DocumentPath!.Replace("\\", "/").Split('/');
            typeNameHint = segments[segments.Length - 1];
        }

        typeNameHint ??= string.Empty;

        if (typeNameHint.IndexOfAny(TypeNameHintCleanupChars) != -1)
        {
            typeNameHint = typeNameHint
                .Replace("[", " Of ")
                .Replace("]", " ")
                .Replace("<", " Of ")
                .Replace(">", " ")
                .Replace(",", " And ")
                .Replace("  ", " ");
            var parts = typeNameHint.Split(' ');
            typeNameHint = string.Join(string.Empty, parts.Select(p => Generate(schema, p)));
        }

        var typeName = RemoveIllegalCharacters(Generate(schema, typeNameHint));
        var reserved = reservedTypeNames as ICollection<string> ?? reservedTypeNames.ToList();
        if (string.IsNullOrEmpty(typeName) || reserved.Contains(typeName))
        {
            typeName = GenerateAnonymousTypeName(typeNameHint, reserved);
        }

        return typeName;
    }

    protected virtual string Generate(ApiSchema schema, string? typeNameHint)
    {
        if (string.IsNullOrEmpty(typeNameHint) && schema.HasTypeNameTitle)
            typeNameHint = schema.Title;

        var lastSegment = GetLastSegment(typeNameHint);
        return ConversionUtilities.ConvertToUpperCamelCase(lastSegment ?? "Anonymous", firstCharacterMustBeAlpha: true);
    }

    private string GenerateAnonymousTypeName(string? typeNameHint, ICollection<string> reserved)
    {
        if (!string.IsNullOrEmpty(typeNameHint))
        {
            typeNameHint = GetLastSegment(typeNameHint);
            typeNameHint = ConversionUtilities.ConvertToUpperCamelCase(typeNameHint, firstCharacterMustBeAlpha: true);
            if (typeNameHint != null &&
                !reserved.Contains(typeNameHint) &&
                Array.IndexOf(reservedTypeNames, typeNameHint) == -1)
            {
                return typeNameHint;
            }

            var count = 1;
            string candidate;
            do
            {
                count++;
                candidate = typeNameHint + count;
            }
            while (reserved.Contains(candidate));

            return candidate;
        }

        return GenerateAnonymousTypeName("Anonymous", reserved);
    }

    private static string? GetLastSegment(string? input)
    {
        if (input == null)
            return null;

        var index = input.LastIndexOf('.');
        return index != -1 ? input.Substring(index + 1) : input;
    }

    private static string RemoveIllegalCharacters(string typeName)
    {
        var hasIllegalCharacters = false;
        for (var i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];
            if (i == 0 && (!IsEnglishLetterOrUnderScore(c) || char.IsDigit(c)))
            {
                hasIllegalCharacters = true;
                break;
            }

            if (!IsEnglishLetterOrUnderScore(c) && !char.IsDigit(c))
            {
                hasIllegalCharacters = true;
                break;
            }
        }

        if (!hasIllegalCharacters)
            return typeName;

        var first = typeName[0];
        var builder = new StringBuilder(typeName);
        if (!IsEnglishLetterOrUnderScore(first) || first == '_')
        {
            if (!NonWordCharacterRegex.IsMatch(first.ToString()))
            {
                builder.Insert(0, "_");
            }
            else
            {
                builder[0] = '_';
            }
        }

        var matches = NonWordCharacterRegex.Matches(builder.ToString());
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            builder[matches[i].Index] = '_';
        }

        return RepeatedUnderscoreRegex.Replace(builder.ToString(), "_").TrimEnd('_');
    }

    private static bool IsEnglishLetterOrUnderScore(char c) =>
        c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
}
