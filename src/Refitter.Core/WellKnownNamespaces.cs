namespace Refitter.Core;

internal static class WellKnownNamespaces
{
    private static readonly string[] ImportedNamespaces =
    {
        "System.Collections.Generic"
    };

    public static string TrimImportedNamespaces(string returnTypeParameter)
    {
        for (int i = 0; i < ImportedNamespaces.Length; i++)
        {
            var ns = ImportedNamespaces[i];
            if (returnTypeParameter.StartsWith(ns, StringComparison.OrdinalIgnoreCase))
            {
                return returnTypeParameter.Replace(ns + ".", string.Empty);
            }
        }
        return returnTypeParameter;
    }
}
