using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
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

            // Create unique hint name based on the full .refitter file path to avoid collisions
            // when multiple .refitter files with the same name exist in different directories
            var hintName = CreateUniqueHintName(file.Path, settings.OutputFilename);

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

    /// <summary>
    /// Creates a unique hint name for AddSource that prevents collisions when multiple
    /// .refitter files with the same name exist in different directories.
    /// </summary>
    /// <param name="refitterFilePath">The full path to the .refitter file</param>
    /// <param name="outputFilename">Optional explicit output filename from settings</param>
    /// <returns>A unique hint name safe for AddSource</returns>
    private static string CreateUniqueHintName(string refitterFilePath, string? outputFilename)
    {
        // If an explicit output filename is set, use it as the base for the hint name
        // but still include path disambiguation to prevent collisions
        var baseName = !string.IsNullOrWhiteSpace(outputFilename)
            ? Path.GetFileNameWithoutExtension(outputFilename)
            : Path.GetFileNameWithoutExtension(refitterFilePath);

        if (string.IsNullOrEmpty(baseName) || baseName == ".")
        {
            baseName = "Refitter";
        }

        // Create a stable unique suffix from the directory path to prevent collisions
        // Use the full path, normalize separators, and create a hash-like identifier
        var directory = Path.GetDirectoryName(refitterFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            // Normalize path separators and compute a stable hash
            var normalizedPath = directory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var pathHash = GetStableHash(normalizedPath);
            return $"{baseName}_{pathHash}.g.cs";
        }

        return $"{baseName}.g.cs";
    }

    /// <summary>
    /// Generates a stable hash string from the input suitable for use in filenames.
    /// Uses a simple but deterministic algorithm.
    /// </summary>
    private static string GetStableHash(string input)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            // Convert to unsigned and format as hex to ensure no negative sign
            return ((uint)hash).ToString("X8");
        }
    }

    private record GeneratedCode(List<Diagnostic> Diagnostics, string? Code = null, string? HintName = null);
}
