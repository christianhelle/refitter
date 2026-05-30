using Refitter.Core;

namespace Refitter;

/// <summary>
/// A single file the generator intends to write: its resolved output path and content.
/// </summary>
internal sealed record PlannedFile(string Path, string Content);

/// <summary>
/// Pure output-path planning for the CLI. Owns every rule that decides
/// <em>where</em> generated code is written — direct CLI output, settings-file
/// rooting, the <c>#1021</c> CLI override, and the contracts-output-folder reroute —
/// without touching the filesystem or the console.
/// </summary>
internal static class OutputPlanner
{
    public static PlannedFile PlanSingleFile(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        string code) =>
        new(GetSingleFileOutputPath(settings, refitGeneratorSettings), code);

    public static IReadOnlyList<PlannedFile> PlanMultipleFiles(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratorOutput generatorOutput)
    {
        var planned = new List<PlannedFile>(generatorOutput.Files.Count);
        foreach (var outputFile in generatorOutput.Files)
        {
            var path = ShouldRerouteToContractsFolder(refitGeneratorSettings, outputFile)
                ? GetContractsOutputPath(settings, refitGeneratorSettings, outputFile)
                : GetMultiFileOutputPath(settings, refitGeneratorSettings, outputFile);

            planned.Add(new PlannedFile(path, outputFile.Content));
        }

        return planned;
    }

    public static string GetSingleFileOutputPath(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        // Direct CLI invocation (no settings file)
        if (UsesDirectCliOutput(settings))
        {
            return settings.OutputPath!;
        }

        if (UsesDirectCliDefaults(settings))
        {
            return Settings.DefaultOutputPath;
        }

        // Settings file mode
        var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

        // Check if CLI explicitly overrides output (#1021 fix)
        var cliOverridesOutput = !string.IsNullOrWhiteSpace(settings.OutputPath) &&
                                  settings.OutputPath != Settings.DefaultOutputPath;

        string outputPath;
        if (cliOverridesOutput)
        {
            // CLI --output overrides settings file
            outputPath = settings.OutputPath!;
        }
        else
        {
            // Use settings file output folder and filename
            var filename = refitGeneratorSettings.OutputFilename ?? "Output.cs";
            outputPath = !string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder)
                ? Path.Combine(refitGeneratorSettings.OutputFolder, filename)
                : filename;
        }

        // Root the output path relative to settings file location if not already rooted
        if (!string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(outputPath))
        {
            outputPath = Path.Combine(root, outputPath);
        }

        return outputPath;
    }

    public static string GetMultiFileOutputPath(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile)
    {
        if (IsDirectCliGeneration(settings))
        {
            var outputDirectory = UsesDirectCliOutput(settings)
                ? settings.OutputPath!
                : ".";

            return Path.Combine(outputDirectory, outputFile.Filename);
        }

        var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

        var outputFolder = HasExplicitCliOutputOverride(settings)
            ? settings.OutputPath
            : refitGeneratorSettings.OutputFolder;

        if (!string.IsNullOrWhiteSpace(outputFolder))
        {
            return CombineWithSettingsRoot(root, outputFolder, outputFile.Filename);
        }

        return CombineWithSettingsRoot(root, outputFile.Filename);
    }

    public static bool ShouldRerouteToContractsFolder(
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile) =>
        !string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder)
        && refitGeneratorSettings.ContractsOutputFolder != RefitGeneratorSettings.DefaultOutputFolder
        && outputFile.Filename == $"{TypenameConstants.Contracts}.cs";

    public static string GetContractsOutputPath(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile)
    {
        var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

        var contractsFolder = Path.GetFullPath(Path.Combine(root, refitGeneratorSettings.ContractsOutputFolder!));
        return Path.Combine(contractsFolder, outputFile.Filename);
    }

    private static bool IsDirectCliGeneration(Settings settings) =>
        string.IsNullOrWhiteSpace(settings.SettingsFilePath);

    private static bool UsesDirectCliOutput(Settings settings) =>
        IsDirectCliGeneration(settings) &&
        !string.IsNullOrWhiteSpace(settings.OutputPath) &&
        settings.OutputPath != Settings.DefaultOutputPath;

    private static bool UsesDirectCliDefaults(Settings settings) =>
        IsDirectCliGeneration(settings) &&
        (string.IsNullOrWhiteSpace(settings.OutputPath) || settings.OutputPath == Settings.DefaultOutputPath);

    private static bool HasExplicitCliOutputOverride(Settings settings) =>
        !string.IsNullOrWhiteSpace(settings.OutputPath) &&
        settings.OutputPath != Settings.DefaultOutputPath;

    private static string CombineWithSettingsRoot(string root, params string[] segments)
    {
        var combinedPath = Path.Combine(segments);
        return !string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(combinedPath)
            ? Path.Combine(root, combinedPath)
            : combinedPath;
    }
}
