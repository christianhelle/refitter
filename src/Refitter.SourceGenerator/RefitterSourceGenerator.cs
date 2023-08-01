using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Refitter.Core;

namespace Refitter.SourceGenerators;

[Generator]
public class RefitterSourceGenerator : IIncrementalGenerator, ISourceGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceFiles = context
            .AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".refitter", StringComparison.InvariantCultureIgnoreCase))
            .Select(GenerateCode);

        context.RegisterSourceOutput(
            sourceFiles,
            (c, file) => c.AddSource(
                GetOutputFilename(file.Settings),
                source: file.Source.ToString()));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        // Method intentionally left empty.
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var jsonFiles =
            context.AdditionalFiles
                .Where(at => at.Path.EndsWith(".refitter", StringComparison.OrdinalIgnoreCase));

        foreach (var file in jsonFiles)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER001",
                        "Refitter",
                        $"Found .refitter file: {file}",
                        "Refitter",
                        DiagnosticSeverity.Info,
                        true),
                    Location.None));

            TryGenerateCode(context, file);
        }
    }

    private static void TryGenerateCode(GeneratorExecutionContext context, AdditionalText file)
    {
        try
        {
            var (settings, sourceText) = GenerateCode(file, context.CancellationToken);

            context.AddSource(GetOutputFilename(settings), sourceText);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER001",
                        "Refitter",
                        "Refitter generated code successfully",
                        "Refitter",
                        DiagnosticSeverity.Info,
                        true),
                    Location.None));
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER000",
                        "Refitter failed to generate code",
                        e.ToString(),
                        "Refitter",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
        }
    }

    private static string GetOutputFilename(RefitGeneratorSettings settings) => $"{settings.Naming.Filename}.cs";

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    private static (RefitGeneratorSettings Settings, SourceText Source) GenerateCode(
        AdditionalText file,
        CancellationToken cancellationToken = default)
    {
        var content = file.GetText(cancellationToken)!;
        var json = content.ToString();
        var settings = JsonConvert.DeserializeObject<RefitGeneratorSettings>(json)!;
        
        Diagnostic.Create(
            new DiagnosticDescriptor(
                "REFITTER001",
                "Refitter File Contents",
                json,
                "Refitter",
                DiagnosticSeverity.Warning,
                true),
            Location.None);

        if (!settings.OpenApiPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !File.Exists(settings.OpenApiPath))
        {
            settings.OpenApiPath = Path.Combine(
                Path.GetDirectoryName(file.Path)!,
                settings.OpenApiPath);
        }

        var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
        var refit = generator.Generate();
        return (settings, SourceText.From(refit, Encoding.UTF8));
    }
}