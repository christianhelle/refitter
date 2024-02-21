using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

internal static class RefitInterfaceImports
{
    private static string[] defaultNamespases = new[]
    {
        "Refit",
        "System.Collections.Generic",
        "System.Text.Json.Serialization",
    };
    public static string[] GetImportedNamespaces(RefitGeneratorSettings settings)
    {
        var namespaces = new List<string>(defaultNamespases);
        if (settings.UseCancellationTokens)
        {
            namespaces.Add("System.Threading");
        }

        if (settings.ReturnIObservable)
        {
            namespaces.Add("System.Reactive");
        }
        else
        {
            namespaces.Add("System.Threading.Tasks");
        }
        return namespaces.ToArray();
    }

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "This tool is cross platform")]
    public static string GenerateNamespaceImports(RefitGeneratorSettings settings) =>
        GetImportedNamespaces(settings)
            .Select(ns => $"using {ns};")
            .Aggregate((a, b) => $"{a}{Environment.NewLine}{b}");
}