using System.Diagnostics.CodeAnalysis;
using Refitter.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
public sealed class GenerateCommand : AsyncCommand<Settings>
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";

    private RefitGeneratorSettings? cachedSettings;

    private static IGenerationReporter CreateReporter(Settings settings) =>
        settings.SimpleOutput
            ? new SimpleGenerationReporter()
            : new RichGenerationReporter();

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!settings.NoLogging)
            Analytics.Configure();

        if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
            context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Success();

        var validationResult = SettingsValidator.Validate(settings, out var refitSettings);
        if (refitSettings != null)
            cachedSettings = refitSettings;

        if (settings.JsonLibraryVersion is { } cliValue &&
            cliValue != 8.0m &&
            refitSettings?.CodeGeneratorSettings?.JsonLibraryVersion is { } fileValue &&
            fileValue != 8.0m)
        {
            return ValidationResult.Error(
                "Cannot specify --json-library-version via CLI when the settings file also specifies a non-default value. Use only one source.");
        }

        return validationResult;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken)
    {
        var reporter = CreateReporter(settings);

        if (!settings.NoLogging)
            Analytics.Configure();

        if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
            context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            return 0;

        RefitGeneratorSettings refitGeneratorSettings;
        if (cachedSettings != null)
        {
            refitGeneratorSettings = cachedSettings;
            cachedSettings = null;
        }
        else if (!string.IsNullOrWhiteSpace(settings.SettingsFilePath))
        {
            var json = await File.ReadAllTextAsync(settings.SettingsFilePath);
            refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);

            if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
                refitGeneratorSettings.OpenApiPath = settings.OpenApiPath;

            ApplySettingsFileDefaults(settings.SettingsFilePath, refitGeneratorSettings);
        }
        else
        {
            refitGeneratorSettings = SettingsMapper.Map(settings);
        }

        var orchestrator = new GenerationOrchestrator();
        return await orchestrator.RunAsync(
            refitGeneratorSettings,
            settings,
            reporter,
            cancellationToken);
    }

    [Obsolete("Use SettingsMapper.Map instead")]
    private static RefitGeneratorSettings CreateRefitGeneratorSettings(Settings settings) =>
        SettingsMapper.Map(settings);

    private static string GetOutputPath(Settings settings, RefitGeneratorSettings refitGeneratorSettings) =>
        OutputPlanner.GetSingleFileOutputPath(settings, refitGeneratorSettings);

    private string GetOutputPath(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile) =>
        OutputPlanner.GetMultiFileOutputPath(settings, refitGeneratorSettings, outputFile);

    internal static void ApplySettingsFileDefaults(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder))
            refitGeneratorSettings.GenerateMultipleFiles = true;

        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
            refitGeneratorSettings.OutputFolder = RefitGeneratorSettings.DefaultOutputFolder;

        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename))
        {
            var refitterFileName = Path.GetFileNameWithoutExtension(settingsFilePath);
            if (string.IsNullOrEmpty(refitterFileName))
                refitterFileName = "Output";
            refitGeneratorSettings.OutputFilename = $"{refitterFileName}.cs";
        }
    }

    internal static string FormatGeneratedFileMarker(string outputPath) =>
        $"{GeneratedFileMarker}{Path.GetFullPath(outputPath)}";

    internal static async Task WriteRefitterSettingsFile(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        var settingsFilePath = DetermineSettingsFilePath(settings);
        var settingsDirectory = Path.GetDirectoryName(settingsFilePath);

        if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
            Directory.CreateDirectory(settingsDirectory);

        var json = Serializer.Serialize(refitGeneratorSettings);
        await File.WriteAllTextAsync(settingsFilePath, json);

        CreateReporter(settings).ReportSettingsFileGenerated(settingsFilePath);
    }

    internal static string DetermineSettingsFilePath(Settings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.OutputPath) &&
            settings.OutputPath != Settings.DefaultOutputPath)
        {
            var outputDir = settings.GenerateMultipleFiles || !string.IsNullOrWhiteSpace(settings.ContractsOutputPath)
                ? settings.OutputPath
                : Path.GetDirectoryName(settings.OutputPath);

            if (!string.IsNullOrWhiteSpace(outputDir))
                return Path.Combine(outputDir, FileExtensionConstants.Refitter);
        }

        return FileExtensionConstants.Refitter;
    }

    internal static void ResolveRelativeSpecPaths(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        var settingsFileDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsFilePath)) ?? string.Empty;
        RefitterSettingsLoader.ResolveRelativeSpecPaths(refitGeneratorSettings, settingsFileDirectory);
    }

    internal static bool IsUrl(string path) =>
        RefitterSettingsLoader.IsUrl(path);
}
