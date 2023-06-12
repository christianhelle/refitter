using System;

namespace Refitter.Core;

public static class RefitInterfaceImports
{
    public static string[] GetImportedNamespaces(RefitGeneratorSettings settings)=>
        settings.UseCancellationTokens
            ? new[]
            {
                "Refit",
                "System.Collections.Generic",
                "System.Text.Json.Serialization",
                "System.Threading",
                "System.Threading.Tasks"
            }
            : new[]
            {
                "Refit",
                "System.Collections.Generic",
                "System.Text.Json.Serialization",
                "System.Threading.Tasks"
            };
    
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