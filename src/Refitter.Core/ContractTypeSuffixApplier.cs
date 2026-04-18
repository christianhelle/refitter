using System.Text.RegularExpressions;

namespace Refitter.Core;

internal static class ContractTypeSuffixApplier
{
    private static readonly Regex TypeDeclarationRegex =
        new(@"(?:public|internal)\s+(?:partial\s+)?(?:class|record|struct)\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private static readonly Regex EnumDeclarationRegex =
        new(@"(?:public|internal)\s+(?:partial\s+)?enum\s+(\w+)", RegexOptions.Multiline | RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public static string ApplySuffix(string generatedCode, string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return generatedCode;

        var typeNames = new HashSet<string>();
        var alreadySuffixedNames = new HashSet<string>();

        // First pass: collect all type names (classes, records, structs, enums)
        // Skip names that already end with the suffix to prevent double-suffixing on rerun.
        foreach (Match match in TypeDeclarationRegex.Matches(generatedCode))
        {
            var name = match.Groups[1].Value;
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                alreadySuffixedNames.Add(name);
            else
                typeNames.Add(name);
        }

        foreach (Match match in EnumDeclarationRegex.Matches(generatedCode))
        {
            var name = match.Groups[1].Value;
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                alreadySuffixedNames.Add(name);
            else
                typeNames.Add(name);
        }

        // Collision detection: if both "Foo" and "FooDto" already exist as declared types,
        // renaming "Foo" to "FooDto" would produce duplicate declarations. Skip those.
        typeNames.RemoveWhere(name => alreadySuffixedNames.Contains(name + suffix));

        // Second pass: replace type names with suffixed versions
        // Sort by length (longest first) to avoid partial replacements
        var orderedTypeNames = typeNames.OrderByDescending(t => t.Length).ToList();
        var result = generatedCode;

        foreach (var typeName in orderedTypeNames)
        {
            var suffixedName = typeName + suffix;
            var escapedName = Regex.Escape(typeName);

            // Replace type declarations
            result = Regex.Replace(
                result,
                $@"(?:public|internal)\s+((?:partial\s+)?(?:class|record|struct|enum))\s+\b{escapedName}\b",
                m => $"{m.Groups[0].Value.Replace(typeName, suffixedName)}",
                RegexOptions.Multiline,
                TimeSpan.FromSeconds(1));

            // Replace type references in inheritance, properties, parameters, etc.
            // The negative lookbehind (?<!{typeName}\s) prevents matching when the token
            // appears as a member name directly after its own type name (e.g., "Foo Foo"
            // or "Foo Foo { get; set; }") — the second "Foo" is the member name and must
            // not be renamed.
            // The negative lookahead (?![a-zA-Z0-9_(]) additionally prevents matching
            // identifiers followed by "(" (e.g., method names such as "Foo()").
            result = Regex.Replace(
                result,
                $@"(?<!{escapedName}[\s])\b{escapedName}\b(?![a-zA-Z0-9_(])",
                suffixedName,
                RegexOptions.None,
                TimeSpan.FromSeconds(1));
        }

        return result;
    }
}
