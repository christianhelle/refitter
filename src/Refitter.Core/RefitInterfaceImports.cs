using System;

namespace Refitter.Core;

public static class RefitInterfaceImports
{
    public static string GenerateNamespaceImports(RefitGeneratorSettings settings) =>
        settings.UseCancellationTokens
            ? string.Join(
                Environment.NewLine,
                "using Refit;",
                "using System.Collections.Generic;",
                "using System.Text.Json.Serialization;",
                "using System.Threading;",
                "using System.Threading.Tasks;")
            : string.Join(
                Environment.NewLine,
                "using Refit;",
                "using System.Collections.Generic;",
                "using System.Text.Json.Serialization;",
                "using System.Threading.Tasks;");
}