using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.CodeAnalysis;

using Refitter.Core;

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

    private static void ProcessResults(SourceProductionContext context, List<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
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
    private static List<Diagnostic> GenerateCode(
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

            var settings = TryDeserialize(json);
            if (settings is null)
            {
                return diagnostics;
            }

            cancellationToken.ThrowIfCancellationRequested();
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
                var filename = settings.OutputFilename ?? Path.GetFileName(file.Path).Replace(".refitter", ".g.cs");
                if (filename == ".g.cs")
                {
                    filename = "Refitter.g.cs";
                }

                var folder = Path.Combine(Path.GetDirectoryName(file.Path)!, settings.OutputFolder);
                var output = Path.Combine(folder, filename);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllText(
                    output,
                    refit,
                    Encoding.UTF8);

                return diagnostics;
            }
            catch (Exception e)
            {
                diagnostics.Add(
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

            return diagnostics;
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

            return diagnostics;
        }
    }

    private static RefitGeneratorSettings? TryDeserialize(string json)
    {
        try
        {
            return Serializer.Deserialize<RefitGeneratorSettings>(json);
        }
        catch
        {
            return null;
        }
    }
}