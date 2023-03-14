using System;

namespace Refitter.Core;

public static class WellKnownNamesspaces
{
    private static readonly string[] wellKnownNamespaces = { "System.Collections.Generic" };

    public static string TrimImportedNamespaces(string returnTypeParameter)
    {
        foreach (var wellKnownNamespace in wellKnownNamespaces)
            if (returnTypeParameter.StartsWith(wellKnownNamespace, StringComparison.OrdinalIgnoreCase))
                return returnTypeParameter.Replace(wellKnownNamespace + ".", string.Empty);
        return returnTypeParameter;
    }
}