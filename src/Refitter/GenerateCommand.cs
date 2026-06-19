using System.Diagnostics.CodeAnalysis;
using Refitter.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
public sealed class GenerateCommand : AsyncCommand<Settings>
{
    private const string GeneratedFileMarker = "GeneratedFile: ";

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
            var json = await File.ReadAllTextAsync(settings.SettingsFilePath, cancellationToken);
            refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);

            if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
                refitGeneratorSettings.OpenApiPath = settings.OpenApiPath;

            RefitterSettingsLoader.ApplyDefaults(settings.SettingsFilePath, refitGeneratorSettings);
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

    internal static string FormatGeneratedFileMarker(string outputPath) =>
        $"{GeneratedFileMarker}{Path.GetFullPath(outputPath)}";

    internal static async Task WriteRefitterSettingsFile(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        CancellationToken cancellationToken = default)
    {
        var settingsFilePath = DetermineSettingsFilePath(settings);
        var settingsDirectory = Path.GetDirectoryName(settingsFilePath);

        if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
            Directory.CreateDirectory(settingsDirectory);

        var json = Serializer.Serialize(refitGeneratorSettings);
        await File.WriteAllTextAsync(settingsFilePath, json, cancellationToken);

        CreateReporter(settings).ReportSettingsFileGenerated(settingsFilePath);
    }

    internal static string DetermineSettingsFilePath(Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.OutputPath) ||
            settings.OutputPath == Settings.DefaultOutputPath)
        {
            return FileExtensionConstants.Refitter;
        }

        var specifyOutputPath = !string.IsNullOrWhiteSpace(settings.ContractsOutputPath);
        var outputDir = settings.GenerateMultipleFiles || specifyOutputPath
            ? settings.OutputPath
            : Path.GetDirectoryName(settings.OutputPath);

        return !string.IsNullOrWhiteSpace(outputDir)
            ? Path.Combine(outputDir, FileExtensionConstants.Refitter)
            : FileExtensionConstants.Refitter;
    }
}
