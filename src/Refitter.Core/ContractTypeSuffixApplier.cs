using System.Text.RegularExpressions;

namespace Refitter.Core;

internal static class ContractTypeSuffixApplier
{
    private static readonly Regex TypeDeclarationRegex =
        new(@"(?:public|internal)\s+(?:partial\s+)?(?:class|record|struct)\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumDeclarationRegex =
        new(@"(?:public|internal)\s+(?:partial\s+)?enum\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled);

    public static string ApplySuffix(string generatedCode, string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return generatedCode;

        var typeNames = new HashSet<string>();

        // First pass: collect all type names (classes, records, structs, enums)
        foreach (Match match in TypeDeclarationRegex.Matches(generatedCode))
        {
            typeNames.Add(match.Groups[1].Value);
        }

        foreach (Match match in EnumDeclarationRegex.Matches(generatedCode))
        {
            typeNames.Add(match.Groups[1].Value);
        }

        // Second pass: replace type names with suffixed versions
        // Sort by length (longest first) to avoid partial replacements
        var orderedTypeNames = typeNames.OrderByDescending(t => t.Length).ToList();
        var result = generatedCode;

        foreach (var typeName in orderedTypeNames)
        {
            var suffixedName = typeName + suffix;

            // Replace type declarations
            result = Regex.Replace(
                result,
                $@"(?:public|internal)\s+((?:partial\s+)?(?:class|record|struct|enum))\s+\b{Regex.Escape(typeName)}\b",
                m => $"{m.Groups[0].Value.Replace(typeName, suffixedName)}",
                RegexOptions.Multiline);

            // Replace type references in inheritance, properties, parameters, etc.
            // Match word boundaries to avoid partial replacements
            result = Regex.Replace(
                result,
                $@"\b{Regex.Escape(typeName)}\b(?![a-zA-Z0-9_])",
                suffixedName);
        }

        return result;
    }
}
