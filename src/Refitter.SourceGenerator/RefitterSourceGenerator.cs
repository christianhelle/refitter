using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Refitter.Core;

namespace Refitter.SourceGenerator;

/// <summary>
/// Source generator for Refitter that generates Refit interfaces from .refitter configuration files.
/// </summary>
[ExcludeFromCodeCoverage]
[Generator(LanguageNames.CSharp)]
public class RefitterSourceGenerator : IIncrementalGenerator
{
    internal const string Category = "Refitter";
    private const string RefitterDiagnosticTitle = Category;

    /// <summary>
    /// Initializes the incremental generator with the necessary configurations.
    /// </summary>
    /// <param name="context">The initialization context for the incremental generator.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var refitterFiles = context
            .AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".refitter", StringComparison.InvariantCultureIgnoreCase));

        var projectDirectory = context
            .AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => GetProjectDirectory(provider.GlobalOptions));

        // collect and sort the paths of the .refitter files for logging
        var refitterPathList = refitterFiles
            .Select((t, _) => t.Path)
            .CollectAsEquatableArray()
            .Select((arr, _) => arr.AsImmutableArray().Sort(StringComparer.InvariantCultureIgnoreCase).AsEquatableArray());

        // add a source output that warns when no .refitter files were found
        context.RegisterSourceOutput(refitterPathList, static (spc, paths) =>
        {
            if (paths.IsEmpty)
            {
                spc.ReportDiagnostic(CreateDiagnostic(CreateNoRefitterFilesFoundDiagnostic()));
            }
        });

        // generate code for each .refitter file and process the results
        context.RegisterImplementationSourceOutput(
            refitterFiles
                .Combine(projectDirectory)
                .Select(static (input, cancellationToken) => GenerateCode(input.Left, input.Right, cancellationToken)),
            ProcessResults);
    }

    private static void ProcessResults(SourceProductionContext context, GeneratedCode result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(CreateDiagnostic(diagnostic));
        }

        if (result.Code is not null && result.HintName is not null)
        {
            context.AddSource(result.HintName, result.Code);
            context.ReportDiagnostic(CreateDiagnostic(CreateGeneratedSuccessfullyDiagnostic(result.HintName)));
        }
    }

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
    internal static GeneratedCode GenerateCode(
        AdditionalText file,
        CancellationToken cancellationToken = default)
    {
        return GenerateCode(file, projectDirectory: null, cancellationToken);
    }

    internal static GeneratedCode GenerateCode(
        AdditionalText file,
        string? projectDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = new List<GeneratedDiagnostic>
        {
            CreateFoundFileDiagnostic(file.Path)
        };

        try
        {
            var json = TryReadRefitterFile(file, diagnostics, cancellationToken);
            if (json is null)
            {
                return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray());
            }

            var settings = TryDeserialize(json, diagnostics);
            if (settings is null)
            {
                return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray());
            }

            cancellationToken.ThrowIfCancellationRequested();
            ResolveRelativeSpecPaths(file.Path, settings);

            if (settings.UseIsoDateFormat &&
                settings.CodeGeneratorSettings?.DateFormat is not null)
            {
                diagnostics.Add(CreateIsoDateFormatOverrideDiagnostic());
            }

            cancellationToken.ThrowIfCancellationRequested();
            var generator = RefitGenerator.CreateAsync(settings).GetAwaiter().GetResult();
            var refit = generator.Generate();

            cancellationToken.ThrowIfCancellationRequested();

            // Create unique hint name based on the project-relative .refitter file path to avoid collisions
            // when multiple .refitter files with the same name exist in different directories
            var hintName = CreateUniqueHintName(file.Path, settings.OutputFilename, projectDirectory);

            return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray(), refit, hintName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            diagnostics.Add(CreateErrorDiagnostic($"Refitter failed to generate code: {e}"));

            return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray());
        }
    }

    private static string? TryReadRefitterFile(
        AdditionalText file,
        List<GeneratedDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = file.GetText(cancellationToken);
            if (content is null)
            {
                diagnostics.Add(CreateErrorDiagnostic($"Unable to read .refitter file: {file.Path}"));
                return null;
            }

            return content.ToString();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            diagnostics.Add(CreateErrorDiagnostic($"Unable to read .refitter file: {file.Path}\n{e}"));
            return null;
        }
    }

    private static string? GetProjectDirectory(AnalyzerConfigOptions globalOptions)
    {
        if (!globalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory) ||
            string.IsNullOrWhiteSpace(projectDirectory))
        {
            globalOptions.TryGetValue("build_property.ProjectDir", out projectDirectory);
        }

        return string.IsNullOrWhiteSpace(projectDirectory)
            ? null
            : projectDirectory;
    }

    private static RefitGeneratorSettings? TryDeserialize(string json, List<GeneratedDiagnostic> diagnostics)
    {
        try
        {
            return Serializer.Deserialize<RefitGeneratorSettings>(json);
        }
        catch (Exception e)
        {
            diagnostics.Add(CreateErrorDiagnostic($"Unable to deserialize .refitter file: {e}"));

            return null;
        }
    }

    private static void ResolveRelativeSpecPaths(string settingsFilePath, RefitGeneratorSettings settings)
    {
        var settingsFileDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsFilePath)) ?? string.Empty;

        var openApiPath = settings.OpenApiPath;
        if (openApiPath is not null &&
            !string.IsNullOrWhiteSpace(openApiPath) &&
            !IsUrl(openApiPath!) &&
            !Path.IsPathRooted(openApiPath))
        {
            settings.OpenApiPath = Path.GetFullPath(Path.Combine(settingsFileDirectory, openApiPath));
        }

        if (settings.OpenApiPaths is { Length: > 0 })
        {
            for (var i = 0; i < settings.OpenApiPaths.Length; i++)
            {
                var path = settings.OpenApiPaths[i];
                if (!string.IsNullOrWhiteSpace(path) &&
                    !IsUrl(path) &&
                    !Path.IsPathRooted(path))
                {
                    settings.OpenApiPaths[i] = Path.GetFullPath(Path.Combine(settingsFileDirectory, path));
                }
            }
        }
    }

    private static bool IsUrl(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    internal static GeneratedDiagnostic CreateNoRefitterFilesFoundDiagnostic() =>
        new(
            "REFITTER003",
            "No .refitter files found",
            "No .refitter files found. Add a `.refitter` file to your project. Refitter.SourceGenerator automatically includes `**/*.refitter` as Roslyn AdditionalFiles via its package props.",
            DiagnosticSeverity.Warning);

    internal static GeneratedDiagnostic CreateGeneratedSuccessfullyDiagnostic(string hintName) =>
        new(
            "REFITTER001",
            RefitterDiagnosticTitle,
            $"{RefitterDiagnosticTitle} generated {hintName} successfully",
            DiagnosticSeverity.Info);

    private static GeneratedDiagnostic CreateFoundFileDiagnostic(string path) =>
        new(
            "REFITTER004",
            RefitterDiagnosticTitle,
            $"Found .refitter File: {path}",
            DiagnosticSeverity.Info);

    private static GeneratedDiagnostic CreateIsoDateFormatOverrideDiagnostic() =>
        new(
            "REFITTER002",
            "Warning",
            "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true",
            DiagnosticSeverity.Warning);

    private static GeneratedDiagnostic CreateErrorDiagnostic(string message) =>
        new(
            "REFITTER000",
            "Error",
            message,
            DiagnosticSeverity.Error);

    private static Diagnostic CreateDiagnostic(GeneratedDiagnostic diagnostic) =>
        Diagnostic.Create(
            new DiagnosticDescriptor(
                diagnostic.Id,
                diagnostic.Title,
                diagnostic.Message,
                Category,
                diagnostic.Severity,
                diagnostic.EnabledByDefault),
            Location.None);

    /// <summary>
    /// Creates a unique hint name for AddSource that prevents collisions when multiple
    /// .refitter files with the same name exist in different directories.
    /// </summary>
    /// <param name="refitterFilePath">The full path to the .refitter file</param>
    /// <param name="outputFilename">Optional explicit output filename from settings</param>
    /// <param name="projectDirectory">Optional MSBuild project directory used to make the hint name stable across machines</param>
    /// <returns>A unique hint name safe for AddSource</returns>
    private static string CreateUniqueHintName(
        string refitterFilePath,
        string? outputFilename,
        string? projectDirectory = null)
    {
        // If an explicit output filename is set, use it as the base for the hint name
        // but still include path disambiguation to prevent collisions
        var baseName = !string.IsNullOrWhiteSpace(outputFilename)
            ? Path.GetFileNameWithoutExtension(outputFilename)
            : Path.GetFileNameWithoutExtension(refitterFilePath);

        if (string.IsNullOrEmpty(baseName) || baseName == ".")
        {
            baseName = RefitterDiagnosticTitle;
        }

        // Create a stable unique suffix from the project-relative .refitter path so files in the same
        // directory can still coexist even when they share the same explicit output filename.
        if (!string.IsNullOrWhiteSpace(refitterFilePath))
        {
            var normalizedPath = GetStablePathForHintName(refitterFilePath, projectDirectory);
            var pathHash = GetStableHash(normalizedPath);
            return $"{baseName}_{pathHash}.g.cs";
        }

        return $"{baseName}.g.cs";
    }

    private static string GetStablePathForHintName(string refitterFilePath, string? projectDirectory)
    {
        var projectDirectoryForHintName = projectDirectory;
        if (!string.IsNullOrWhiteSpace(projectDirectoryForHintName) &&
            TryGetRelativePath(projectDirectoryForHintName!, refitterFilePath, out var relativePath) &&
            relativePath is not null)
        {
            return NormalizePathForHash(relativePath);
        }

        return NormalizePathForHash(Path.GetFullPath(refitterFilePath));
    }

    private static bool TryGetRelativePath(string projectDirectory, string filePath, out string? relativePath)
    {
        relativePath = null;

        try
        {
            var projectUri = new Uri(EnsureTrailingDirectorySeparator(Path.GetFullPath(projectDirectory)));
            var fileUri = new Uri(Path.GetFullPath(filePath));

            if (!projectUri.IsBaseOf(fileUri))
            {
                return false;
            }

            relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(fileUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);

            return !string.IsNullOrWhiteSpace(relativePath);
        }
        catch (UriFormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
            path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static string NormalizePathForHash(string path)
    {
        return path
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .Replace(Path.DirectorySeparatorChar, '/');
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

    internal readonly record struct GeneratedCode(
        EquatableArray<GeneratedDiagnostic> Diagnostics,
        string? Code = null,
        string? HintName = null);

    [SuppressMessage(
        "Major Code Smell",
        "S1206:Equals(object) and GetHashCode() should be overridden in pairs",
        Justification = "readonly record struct synthesizes the paired Equals overloads; only the hash code is customized to keep ordinal semantics explicit.")]
    internal readonly record struct GeneratedDiagnostic : IEquatable<GeneratedDiagnostic>
    {
        public GeneratedDiagnostic(
            string id,
            string title,
            string message,
            DiagnosticSeverity severity,
            bool enabledByDefault = true)
        {
            Id = id;
            Title = title;
            Message = message;
            Severity = severity;
            EnabledByDefault = enabledByDefault;
        }

        public string Id { get; }

        public string Title { get; }

        public string Message { get; }

        public DiagnosticSeverity Severity { get; }

        public bool EnabledByDefault { get; }

        public override int GetHashCode()
        {
            HashCode hashCode = default;
            hashCode.Add(Id, StringComparer.Ordinal);
            hashCode.Add(Title, StringComparer.Ordinal);
            hashCode.Add(Message, StringComparer.Ordinal);
            hashCode.Add((int)Severity);
            hashCode.Add(EnabledByDefault);
            return hashCode.ToHashCode();
        }
    }
}
