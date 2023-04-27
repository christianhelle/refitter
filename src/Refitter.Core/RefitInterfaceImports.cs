using System;

namespace Refitter.Core;

public static class RefitInterfaceImports
{
    public static string GenerateNamespaceImports(RefitGeneratorSettings settings) =>
        settings.UseCancellationTokens
            ? string.Join(
                Environment.NewLine,
                "using Refit;",
                "using System.Threading;",
                "using System.Threading.Tasks;",
                "using System.Collections.Generic;")
            : string.Join(
                Environment.NewLine,
                "using Refit;",
                "using System.Threading.Tasks;",
                "using System.Collections.Generic;");
}