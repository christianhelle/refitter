using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    internal static readonly DiagnosticDescriptor NoRefitterFilesDescriptor =
        new(
            "REFITTER003",
            "Refitter",
            "No .refitter files were found. Add one as an AdditionalFiles item, for example: <AdditionalFiles Include=\"Petstore.refitter\" />",
            "Refitter",
            DiagnosticSeverity.Warning,
            true);

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
                spc.ReportDiagnostic(Diagnostic.Create(NoRefitterFilesDescriptor, Location.None));
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

    private static void ProcessResults(SourceProductionContext context, GeneratedSourceResult result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
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
    private static GeneratedSourceResult GenerateCode(
        AdditionalText file,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        diagnostics.Add(
            new DiagnosticInfo(
                "REFITTER001",
                "Refitter",
                $"Found .refitter File: {file.Path}",
                "Refitter",
                DiagnosticSeverity.Info,
                true));

        try
        {
            var content = file.GetText(cancellationToken)!;
            var json = content.ToString();

            diagnostics.Add(
                new DiagnosticInfo(
                    "REFITTER001",
                    "Refitter File Contents",
                    json,
                    "Refitter",
                    DiagnosticSeverity.Info,
                    true));

            var settings = TryDeserialize(json, diagnostics);
            if (settings is null)
            {
                return new GeneratedSourceResult(diagnostics.ToImmutable());
            }

            cancellationToken.ThrowIfCancellationRequested();
            ResolveOpenApiSpecPaths(settings, file.Path);

            if (settings.UseIsoDateFormat &&
                settings.CodeGeneratorSettings?.DateFormat is not null)
            {
                diagnostics.Add(
                    new DiagnosticInfo(
                        "REFITTER002",
                        "Warning",
                        "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true",
                        "Refitter",
                        DiagnosticSeverity.Warning,
                        true));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
            var refit = generator.Generate();

            cancellationToken.ThrowIfCancellationRequested();

            // Create unique hint name based on the full .refitter file path to avoid collisions
            // when multiple .refitter files with the same name exist in different directories
            var hintName = CreateUniqueHintName(file.Path, settings.OutputFilename);

            return new GeneratedSourceResult(diagnostics.ToImmutable(), refit, hintName);
        }
        catch (Exception e)
        {
            diagnostics.Add(
                new DiagnosticInfo(
                    "REFITTER000",
                    "Error",
                    $"Refitter failed to generate code: {e}",
                    "Refitter",
                    DiagnosticSeverity.Error,
                    true));

            return new GeneratedSourceResult(diagnostics.ToImmutable());
        }
    }

    private static RefitGeneratorSettings? TryDeserialize(
        string json,
        ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        try
        {
            return Serializer.Deserialize<RefitGeneratorSettings>(json);
        }
        catch (Exception e)
        {
            diagnostics.Add(
                new DiagnosticInfo(
                    "REFITTER000",
                    "Error",
                    $"Unable to deserialize .refitter file: {e}",
                    "Refitter",
                    DiagnosticSeverity.Info,
                    true
                )
            );

            return null;
        }
    }

    /// <summary>
    /// Creates a unique hint name for AddSource that prevents collisions when multiple
    /// .refitter files resolve to the same output filename.
    /// </summary>
    /// <param name="refitterFilePath">The full path to the .refitter file</param>
    /// <param name="outputFilename">Optional explicit output filename from settings</param>
    /// <returns>A unique hint name safe for AddSource</returns>
    private static string CreateUniqueHintName(string refitterFilePath, string? outputFilename)
    {
        // If an explicit output filename is set, use it as the base for the hint name
        // but still include path disambiguation to prevent collisions.
        var baseName = !string.IsNullOrWhiteSpace(outputFilename)
            ? Path.GetFileNameWithoutExtension(outputFilename)
            : Path.GetFileNameWithoutExtension(refitterFilePath);

        if (string.IsNullOrEmpty(baseName) || baseName == ".")
        {
            baseName = "Refitter";
        }

        var normalizedPath = Path.GetFullPath(refitterFilePath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var pathHash = GetStableHash(normalizedPath);
        return $"{baseName}_{pathHash}.g.cs";
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

    private static void ResolveOpenApiSpecPaths(RefitGeneratorSettings settings, string refitterFilePath)
    {
        if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
        {
            settings.OpenApiPath = ResolveOpenApiSpecPath(settings.OpenApiPath, refitterFilePath);
        }

        if (settings.OpenApiPaths is not { Length: > 0 })
        {
            return;
        }

        for (var i = 0; i < settings.OpenApiPaths.Length; i++)
        {
            settings.OpenApiPaths[i] = ResolveOpenApiSpecPath(settings.OpenApiPaths[i], refitterFilePath);
        }
    }

    private static string? ResolveOpenApiSpecPath(string? openApiPath, string refitterFilePath)
    {
        if (string.IsNullOrWhiteSpace(openApiPath) ||
            openApiPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            openApiPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            Path.IsPathRooted(openApiPath))
        {
            return openApiPath;
        }

        var root = Path.GetDirectoryName(Path.GetFullPath(refitterFilePath)) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(root, openApiPath));
    }

    internal readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public DiagnosticInfo(
            string id,
            string title,
            string message,
            string category,
            DiagnosticSeverity severity,
            bool isEnabledByDefault)
        {
            Id = id;
            Title = title;
            Message = message;
            Category = category;
            Severity = severity;
            IsEnabledByDefault = isEnabledByDefault;
        }

        public string Id { get; }

        public string Title { get; }

        public string Message { get; }

        public string Category { get; }

        public DiagnosticSeverity Severity { get; }

        public bool IsEnabledByDefault { get; }

        public Diagnostic ToDiagnostic() =>
            Diagnostic.Create(
                new DiagnosticDescriptor(Id, Title, Message, Category, Severity, IsEnabledByDefault),
                Location.None);

        public bool Equals(DiagnosticInfo other) =>
            string.Equals(Id, other.Id, StringComparison.Ordinal) &&
            string.Equals(Title, other.Title, StringComparison.Ordinal) &&
            string.Equals(Message, other.Message, StringComparison.Ordinal) &&
            string.Equals(Category, other.Category, StringComparison.Ordinal) &&
            Severity == other.Severity &&
            IsEnabledByDefault == other.IsEnabledByDefault;

        public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Id);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Title);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Message);
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Category);
                hash = (hash * 31) + Severity.GetHashCode();
                hash = (hash * 31) + IsEnabledByDefault.GetHashCode();
                return hash;
            }
        }
    }

    internal readonly struct GeneratedSourceResult : IEquatable<GeneratedSourceResult>
    {
        public GeneratedSourceResult(
            ImmutableArray<DiagnosticInfo> diagnostics,
            string? code = null,
            string? hintName = null)
        {
            Diagnostics = diagnostics;
            Code = code;
            HintName = hintName;
        }

        public ImmutableArray<DiagnosticInfo> Diagnostics { get; }

        public string? Code { get; }

        public string? HintName { get; }

        public bool Equals(GeneratedSourceResult other) =>
            string.Equals(Code, other.Code, StringComparison.Ordinal) &&
            string.Equals(HintName, other.HintName, StringComparison.Ordinal) &&
            Diagnostics.SequenceEqual(other.Diagnostics);

        public override bool Equals(object? obj) => obj is GeneratedSourceResult other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + (Code is null ? 0 : StringComparer.Ordinal.GetHashCode(Code));
                hash = (hash * 31) + (HintName is null ? 0 : StringComparer.Ordinal.GetHashCode(HintName));

                foreach (var diagnostic in Diagnostics)
                {
                    hash = (hash * 31) + diagnostic.GetHashCode();
                }

                return hash;
            }
        }
    }
}
