using System;

namespace Refitter.Core;

public static class RefitInterfaceImports
{
    public static string GenerateNamespaceImports() =>
        string.Join(
            Environment.NewLine,
            "using Refit;",
            "using System.Threading;",
            "using System.Threading.Tasks;",
            "using System.Collections.Generic;");
}