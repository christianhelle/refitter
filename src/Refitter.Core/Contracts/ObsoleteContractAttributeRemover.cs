using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Removes <c>[System.Obsolete]</c> from deprecated contract types that the generated Refit interfaces use.
/// The contracts suppress CS0612, but the interfaces and the stubs Refit's source generator emits for them
/// cannot, so builds with TreatWarningsAsErrors would fail (#1278).
/// </summary>
internal static class ObsoleteContractAttributeRemover
{
    private static readonly Regex ObsoleteTypeAttributeRegex = new(
        @"^[ \t]*\[System\.Obsolete(?:\([^\r\n]*\))?\][ \t]*\r?\n" +
        @"(?=(?:[ \t]*\[[^\r\n]*\][ \t]*\r?\n)*[ \t]*(?:public|internal) (?:(?:abstract|sealed|partial) )*(?:class|record|struct|enum) (?<name>\w+))",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(5));

    private static readonly Regex IdentifierRegex = new(
        @"\b[A-Za-z_]\w*\b",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static string Remove(string contracts, IEnumerable<string> interfaceCode)
    {
        var referencedIdentifiers = new HashSet<string>(
            interfaceCode.SelectMany(code => IdentifierRegex.Matches(code).Cast<Match>().Select(m => m.Value)),
            StringComparer.Ordinal);

        return ObsoleteTypeAttributeRegex.Replace(
            contracts,
            match => referencedIdentifiers.Contains(match.Groups["name"].Value)
                ? string.Empty
                : match.Value);
    }
}
