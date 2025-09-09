using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Refitter.Core;

namespace Refitter.SourceGenerator;

/// <summary>
/// Source generator for Refitter that generates Refit interfaces from .refitter configuration files.
/// </summary>
[ExcludeFromCodeCoverage]
[Generator(LanguageNames.CSharp)]
public class RefitterSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator with the necessary configurations.
    /// </summary>
    /// <param name="context">The initialization context for the incremental generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var refitterFiles = context
            .AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".refitter", StringComparison.InvariantCultureIgnoreCase));

        // collect and sort the paths of the .refitter files for logging
        var refitterPathList = refitterFiles
            .Select((t, _) => t.Path)
            .Collect()
            .Select((arr, _) => arr.Sort(StringComparer.InvariantCultureIgnoreCase));

        // add a source output that logs what we found for easier troubleshooting and setup
        context.RegisterSourceOutput(refitterPathList, static (spc, paths) =>
        {
            if (paths.Length == 0)
            {
                // log a warning if no .refitter files were found, instructing the user how to add them
                Debug.WriteLine("[Refitter] No .refitter files found. Ensure they are added to your project as `<AdditionalFiles Include=\"Petstore.refitter\" />`");
                return;
            }

            // log each found .refitter file path
            foreach (var path in paths)
            {
                Debug.WriteLine($"[Refitter] Found .refitter file: {path}");
            }
        });

        // generate code for each .refitter file and process the results
        context.RegisterImplementationSourceOutput(refitterFiles.Select(GenerateCode), ProcessResults);
    }


    private static void ProcessResults(SourceProductionContext context, GeneratedCode result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (result.Code is not null && result.HintName is not null)
        {
            context.AddSource(result.HintName, result.Code);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "REFITTER001",
                        "Refitter",
                        $"Refitter generated {result.HintName} successfully",
                        "Refitter",
                        DiagnosticSeverity.Info,
                        true),
                    Location.None));
        }
    }

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    private static GeneratedCode GenerateCode(
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

            var settings = TryDeserialize(json, diagnostics);
            if (settings is null)
            {
                return new GeneratedCode(diagnostics);
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

            cancellationToken.ThrowIfCancellationRequested();
            var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
            var refit = generator.Generate();

            cancellationToken.ThrowIfCancellationRequested();
            var filename = settings.OutputFilename ?? Path.GetFileName(file.Path).Replace(".refitter", ".g.cs");
            if (filename == ".g.cs")
            {
                filename = "Refitter.g.cs";
            }

            // Create unique hint name based on the .refitter file path to avoid collisions
            var hintName = Path.GetFileNameWithoutExtension(file.Path);
            if (string.IsNullOrEmpty(hintName) || hintName == ".")
            {
                hintName = "Refitter";
            }
            hintName = hintName + ".g.cs";

            return new GeneratedCode(diagnostics, refit, hintName);
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

            return new GeneratedCode(diagnostics);
        }
    }

    private static RefitGeneratorSettings? TryDeserialize(string json, List<Diagnostic> diagnostics)
    {
        try
        {
            return Serializer.Deserialize<RefitGeneratorSettings>(json);
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

    private record GeneratedCode(List<Diagnostic> Diagnostics, string? Code = null, string? HintName = null);
}
