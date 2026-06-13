using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using H.Generators.Extensions;
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
        context.RegisterImplementationSourceOutput(refitterFiles.Select(GenerateCode), ProcessResults);
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

    internal static GeneratedCode GenerateCode(
        AdditionalText file,
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

            diagnostics.Add(CreateFileContentsDiagnostic(json));

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

            var hintName = CreateUniqueHintName(file.Path, settings.OutputFilename);

            return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray(), refit, hintName);
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
        catch (Exception e)
        {
            diagnostics.Add(CreateErrorDiagnostic($"Unable to read .refitter file: {file.Path}{Environment.NewLine}{e}"));
            return null;
        }
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
        var lastSep = settingsFilePath.LastIndexOfAny(new[] { '/', '\\' });
        var settingsFileDirectory = lastSep >= 0 ? settingsFilePath.Substring(0, lastSep) : string.Empty;
        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, settingsFileDirectory);
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

    private static GeneratedDiagnostic CreateFileContentsDiagnostic(string json) =>
        new(
            "REFITTER005",
            "Refitter File Contents",
            json,
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

    private static string CreateUniqueHintName(string refitterFilePath, string? outputFilename)
    {
        var baseName = !string.IsNullOrWhiteSpace(outputFilename)
            ? GetFileNameWithoutExtension(outputFilename!)
            : GetFileNameWithoutExtension(refitterFilePath);

        if (string.IsNullOrEmpty(baseName) || baseName == ".")
        {
            baseName = RefitterDiagnosticTitle;
        }

        if (!string.IsNullOrWhiteSpace(refitterFilePath))
        {
            var normalizedPath = refitterFilePath.Replace('/', '\\');
            var pathHash = GetStableHash(normalizedPath);
            return $"{baseName}_{pathHash}.g.cs";
        }

        return $"{baseName}.g.cs";
    }

    private static string GetFileNameWithoutExtension(string path)
    {
        var lastSep = path.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = lastSep >= 0 ? path.Substring(lastSep + 1) : path;
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
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
