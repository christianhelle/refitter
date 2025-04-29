using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Refitter.Core;
using Refitter.SourceGenerator.Models;

namespace Refitter.SourceGenerator;

[ExcludeFromCodeCoverage]
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

    private static void ProcessResults(SourceProductionContext context, GeneratedCodeResult result)
    {

        if (string.IsNullOrEmpty(result.GeneratedCode))
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
            return;
        }
        context.CancellationToken.ThrowIfCancellationRequested();
        try
        {

            var filename = result.OutputFilename ?? Path.GetFileName(result.ConfigFile.Path).Replace(".refitter", ".g.cs");
            if (filename == ".g.cs")
            {
                filename = "Refitter.g.cs";
            }
            var folder = Path.Combine(Path.GetDirectoryName(result.ConfigFile.Path)!, "Generated");
            var output = Path.Combine(folder, filename);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            if (result.GenerateVisibilFile == true)
            {
                File.WriteAllText(
                    output,
                    result.GeneratedCode,
                    Encoding.UTF8
                );
            }
            else
            {
                context.AddSource(
                    output,
                    // the compilere thinks the GeneratedCode property is null, but it is not.
                    // It's already checked in the first statement of this method.
                    result.GeneratedCode!);
            }

        }
        catch (Exception e)
        {
            result.Diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER000",
                        "Error",
                        $"Refitter failed to write generated code: {e}",
                        "Refitter",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
        }

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
    private static GeneratedCodeResult GenerateCode(
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
            var settings = ExtractSettings(file, diagnostics, cancellationToken);
            if (settings is null)
            {
                return new GeneratedCodeResult() { Diagnostics = diagnostics, ConfigFile = file };
            }
            cancellationToken.ThrowIfCancellationRequested();
            var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
            // TODO: currently even if `settings.generateMultipleFiles` is set, it will only generate a single file,
            // if the source generator was the one who generated the code.
            var refit = generator.Generate();
            return new GeneratedCodeResult()
            {
                Diagnostics = diagnostics,
                GeneratedCode = refit,
                OutputFilename = settings.OutputFilename,
                GenerateVisibilFile = settings.GenerateVisibilFile,
                ConfigFile = file
            };
        }
        catch (Exception e)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER000",
                        "Error",
                        $"Refitter failed to generate code: {e}",
                        "Refitter",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));

            return new GeneratedCodeResult() { Diagnostics = diagnostics, ConfigFile = file };
        }
    }

    private static RefitSourceGeneratorSettings? ExtractSettings(
        AdditionalText file,
        List<Diagnostic> diagnostics,
        CancellationToken cancellationToken = default)
    {
        var content = file.GetText(cancellationToken)!;
        var json = content.ToString();

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

        var settings = TryDeserialize(json, diagnostics);
        if (settings is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!settings.OpenApiPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
            !File.Exists(settings.OpenApiPath))
        {
            settings.OpenApiPath = Path.Combine(
                Path.GetDirectoryName(file.Path)!,
                settings.OpenApiPath);
        }

        if (settings.UseIsoDateFormat &&
            settings.CodeGeneratorSettings?.DateFormat is not null)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER002",
                        "Warning",
                        "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true",
                        "Refitter",
                        DiagnosticSeverity.Warning,
                        true),
                    Location.None));
        }

        return settings;
    }

    private static RefitSourceGeneratorSettings? TryDeserialize(string json, List<Diagnostic> diagnostics)
    {
        try
        {
            return Serializer.Deserialize<RefitSourceGeneratorSettings>(json);
        }
        catch (Exception e)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER000",
                        "Error",
                        $"Unable to deserialize .refitter file: {e}",
                        "Refitter",
                        DiagnosticSeverity.Info,
                        true
                    ),
                    Location.None
                )
            );

            return null;
        }
    }

    class GeneratedCodeResult
    {
        public string? GeneratedCode { get; init; }
        public string? OutputFilename { get; init; }
        public required AdditionalText ConfigFile { get; init; }
        public bool? GenerateVisibilFile { get; init; }
        public required List<Diagnostic> Diagnostics { get; init; } = new List<Diagnostic>();
    }
}
