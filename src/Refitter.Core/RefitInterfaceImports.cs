using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Refitter.Core;

internal static class RefitInterfaceImports
{
    private static readonly string[] defaultNamespases = new[]
    {
        "Refit",
        "System.Collections.Generic",
        "System.Text.Json.Serialization",
    };
    public static string[] GetImportedNamespaces(RefitGeneratorSettings settings)
    {
        var namespaces = new List<string>(defaultNamespases);

        if(settings.GenerateDisposableClients)
        {
            namespaces.Add("System");
        }

        if (settings.ApizrSettings?.WithRequestOptions == true)
        {
            namespaces.Add("Apizr.Configuring.Request");
        }
        else if (settings.UseCancellationTokens)
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

        if (settings.ExcludeNamespaces.Length != 0)
        {
            var exclusionNamespacesRegexes = settings.ExcludeNamespaces
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(x => new Regex(x, RegexOptions.Compiled))
                .ToList();
            
            var excludedNamespaces = exclusionNamespacesRegexes.SelectMany(k => namespaces.Where(x => k.IsMatch(x)));
            namespaces = namespaces.Except(excludedNamespaces).ToList();
        }

        if (settings.GenerateMultipleFiles && settings.ContractsNamespace is not null)
        {
            namespaces.Add(settings.ContractsNamespace);
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