using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Newtonsoft.Json;

using Refitter.Core;

namespace Refitter.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class RefitterSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceFiles = context
            .AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".refitter", StringComparison.InvariantCultureIgnoreCase))
            .Select(GenerateCode);

        context.RegisterImplementationSourceOutput(sourceFiles, ProcessResults);
    }

    private static void ProcessResults(SourceProductionContext context, GenerateCodeResult result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

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

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    private static GenerateCodeResult GenerateCode(
        AdditionalText file,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = new List<Diagnostic>
        {
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "REFITTER001",
                    "Refitter",
                    $"Found .refitter File: {file.Path}",
                    "Refitter",
                    DiagnosticSeverity.Info,
                    true),
                Location.None)
        };

        try
        {
            var filename = Path.GetFileName(file.Path).Replace(".refitter", ".g.cs");
            if (filename == ".g.cs")
            {
                filename = "Refitter.g.cs";
            }

            var content = file.GetText(cancellationToken)!;
            var json = content.ToString();
            var settings = JsonConvert.DeserializeObject<RefitGeneratorSettings>(json)!;
            cancellationToken.ThrowIfCancellationRequested();

            diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER001",
                        "Refitter File Contents",
                        json,
                        "Refitter",
                        DiagnosticSeverity.Info,
                        true),
                    Location.None));

            if (!settings.OpenApiPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                !File.Exists(settings.OpenApiPath))
            {
                settings.OpenApiPath = Path.Combine(
                    Path.GetDirectoryName(file.Path)!,
                    settings.OpenApiPath);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
            var refit = generator.Generate();

            cancellationToken.ThrowIfCancellationRequested();
            try 
            {
                var folder = Path.Combine(Path.GetDirectoryName(file.Path), "Generated");
                var output = Path.Combine(folder, filename);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(
                    output, 
                    refit, 
                    Encoding.UTF8);
                    
                return new(false, null, null, diagnostics);
            }
            catch (Exception e)
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "REFITTER000",
                            "Refitter failed to write generated code",
                            e.ToString(),
                            "Refitter",
                            DiagnosticSeverity.Error,
                            true),
                        Location.None));
            }

            return new(true, SourceText.From(refit, Encoding.UTF8), filename, diagnostics);
        }
        catch (Exception e)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER000",
                        "Refitter failed to generate code",
                        e.ToString(),
                        "Refitter",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));

            return new(false, null, null, diagnostics);
        }
    }

    private record GenerateCodeResult(
        bool Success,
        SourceText? Source,
        string? Filename,
        List<Diagnostic> Diagnostics)
    {
        public bool Success { get; } = Success;
        public SourceText? Source { get; } = Source;
        public string? Filename { get; } = Filename;
        public List<Diagnostic> Diagnostics { get; } = Diagnostics;
    }
}