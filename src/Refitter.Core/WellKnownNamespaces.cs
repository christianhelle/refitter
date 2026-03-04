namespace Refitter.Core;

internal static class WellKnownNamespaces
{
    private static readonly string[] ImportedNamespaces =
    {
        "System.Collections.Generic"
    };

    public static string TrimImportedNamespaces(string returnTypeParameter) =>
        ImportedNamespaces
            .Where(s => returnTypeParameter.StartsWith(s, StringComparison.OrdinalIgnoreCase))
            .Select(s => returnTypeParameter.Replace(s + ".", string.Empty))
            .FirstOrDefault() ??
        returnTypeParameter;
}
