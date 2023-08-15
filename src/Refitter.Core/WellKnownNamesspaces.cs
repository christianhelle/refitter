using System;

namespace Refitter.Core;

internal static class WellKnownNamesspaces
{
    private static readonly string[] WellKnownNamespaces =
    {
        "System.Collections.Generic"
    };

    public static string TrimImportedNamespaces(string returnTypeParameter) =>
        WellKnownNamespaces
            .Where(s => returnTypeParameter.StartsWith(s, StringComparison.OrdinalIgnoreCase))
            .Select(s => returnTypeParameter.Replace(s + ".", string.Empty))
            .FirstOrDefault() ??
        returnTypeParameter;
}