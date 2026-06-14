using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
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
    }

    [SuppressMessage(
        "MicrosoftCodeAnalysisCorrectness",
        "RS1035:Do not use APIs banned for analyzers",
        Justification = "By design")]
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

            var outputPath = GetOutputPath(file.Path, settings);
            WriteGeneratedFile(outputPath, refit, diagnostics);

            return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray(), outputPath);
        }
        catch (Exception e)
        {
            diagnostics.Add(CreateErrorDiagnostic($"Refitter failed to generate code: {e}"));

            return new GeneratedCode(diagnostics.ToImmutableArray().AsEquatableArray());
        }

        static string GetOutputPath(string refitterFilePath, RefitGeneratorSettings settings)
        {
            var directory = GetDirectoryName(refitterFilePath);
            var folder = Path.Combine(directory, settings.OutputFolder);
            var filename = !string.IsNullOrWhiteSpace(settings.OutputFilename)
                ? settings.OutputFilename
                : GetFileNameWithoutExtension(refitterFilePath) + ".g.cs";
            return Path.Combine(folder, filename);
        }

        static void WriteGeneratedFile(string outputPath, string content, List<GeneratedDiagnostic> diagnostics)
        {
            try
            {
                var folder = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var existingContent = File.Exists(outputPath) ? File.ReadAllText(outputPath, Encoding.UTF8) : null;
                if (existingContent == null || !existingContent.Equals(content, StringComparison.Ordinal))
                {
                    File.WriteAllText(outputPath, content, Encoding.UTF8);
                }

                diagnostics.Add(CreateGeneratedSuccessfullyDiagnostic(outputPath));
            }
            catch (Exception e)
            {
                diagnostics.Add(CreateErrorDiagnostic($"Refitter failed to write generated code: {e}"));
            }
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
        string settingsFileDirectory;
        if (lastSep < 0)
        {
            settingsFileDirectory = string.Empty;
        }
        else if (lastSep == 0)
        {
            settingsFileDirectory = settingsFilePath.Substring(0, 1);
        }
        else if (lastSep == 2 && settingsFilePath.Length > 1 && settingsFilePath[1] == ':')
        {
            settingsFileDirectory = settingsFilePath.Substring(0, lastSep + 1);
        }
        else
        {
            settingsFileDirectory = settingsFilePath.Substring(0, lastSep);
        }
        RefitterSettingsLoader.ResolveRelativeSpecPaths(settings, settingsFileDirectory);
    }

    internal static GeneratedDiagnostic CreateNoRefitterFilesFoundDiagnostic() =>
        new(
            "REFITTER003",
            "No .refitter files found",
            "No .refitter files found. Add a `.refitter` file to your project. Refitter.SourceGenerator automatically includes `**/*.refitter` as Roslyn AdditionalFiles via its package props.",
            DiagnosticSeverity.Warning);

    internal static GeneratedDiagnostic CreateGeneratedSuccessfullyDiagnostic(string outputPath) =>
        new(
            "REFITTER001",
            RefitterDiagnosticTitle,
            $"{RefitterDiagnosticTitle} generated {outputPath} successfully",
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

    private static string GetDirectoryName(string path)
    {
        var lastSep = path.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSep < 0)
        {
            return string.Empty;
        }
        else if (lastSep == 0)
        {
            return path.Substring(0, 1);
        }
        else if (lastSep == 2 && path.Length > 1 && path[1] == ':')
        {
            return path.Substring(0, lastSep + 1);
        }
        else
        {
            return path.Substring(0, lastSep);
        }
    }

    private static string GetFileNameWithoutExtension(string path)
    {
        var lastSep = path.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = lastSep >= 0 ? path.Substring(lastSep + 1) : path;
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
    }

    internal readonly record struct GeneratedCode(
        EquatableArray<GeneratedDiagnostic> Diagnostics,
        string? OutputPath = null);

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
