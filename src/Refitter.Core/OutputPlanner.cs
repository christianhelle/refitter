namespace Refitter.Core;

public sealed record PlannedFile(string Path, string Content);

public static class OutputPlanner
{
    public const string DefaultOutputPath = "Output.cs";

    public static PlannedFile PlanSingleFile(
        string? settingsFilePath,
        string? cliOutputPath,
        RefitGeneratorSettings refitGeneratorSettings,
        string code) =>
        new(GetSingleFileOutputPath(settingsFilePath, cliOutputPath, refitGeneratorSettings), code);

    public static IReadOnlyList<PlannedFile> PlanMultipleFiles(
        string? settingsFilePath,
        string? cliOutputPath,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratorOutput generatorOutput)
    {
        var planned = new List<PlannedFile>(generatorOutput.Files.Count);
        foreach (var outputFile in generatorOutput.Files)
        {
            var path = ShouldRerouteToContractsFolder(refitGeneratorSettings, outputFile)
                ? GetContractsOutputPath(settingsFilePath, refitGeneratorSettings, outputFile)
                : GetMultiFileOutputPath(settingsFilePath, cliOutputPath, refitGeneratorSettings, outputFile);

            planned.Add(new PlannedFile(path, outputFile.Content));
        }

        return planned;
    }

    public static string GetSingleFileOutputPath(
        string? settingsFilePath,
        string? cliOutputPath,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        if (IsDirectCliGeneration(settingsFilePath))
        {
            if (HasExplicitCliOutputOverride(cliOutputPath))
                return cliOutputPath!;

            return DefaultOutputPath;
        }

        var root = string.IsNullOrWhiteSpace(settingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settingsFilePath) ?? string.Empty;

        var cliOverridesOutput = HasExplicitCliOutputOverride(cliOutputPath);

        string outputPath;
        if (cliOverridesOutput)
        {
            outputPath = cliOutputPath!;
        }
        else
        {
            var filename = refitGeneratorSettings.OutputFilename ?? "Output.cs";
            outputPath = !string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder)
                ? Path.Combine(refitGeneratorSettings.OutputFolder, filename)
                : filename;
        }

        if (!string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(outputPath))
            outputPath = Path.Combine(root, outputPath);

        return outputPath;
    }

    public static string GetMultiFileOutputPath(
        string? settingsFilePath,
        string? cliOutputPath,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile)
    {
        if (IsDirectCliGeneration(settingsFilePath))
        {
            var outputDirectory = HasExplicitCliOutputOverride(cliOutputPath)
                ? cliOutputPath!
                : ".";

            return Path.Combine(outputDirectory, outputFile.Filename);
        }

        var root = string.IsNullOrWhiteSpace(settingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settingsFilePath) ?? string.Empty;

        var outputFolder = HasExplicitCliOutputOverride(cliOutputPath)
            ? cliOutputPath
            : refitGeneratorSettings.OutputFolder;

        if (!string.IsNullOrWhiteSpace(outputFolder))
            return CombineWithSettingsRoot(root, outputFolder!, outputFile.Filename);

        return CombineWithSettingsRoot(root, outputFile.Filename);
    }

    public static bool ShouldRerouteToContractsFolder(
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile) =>
        !string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder)
        && refitGeneratorSettings.ContractsOutputFolder != RefitGeneratorSettings.DefaultOutputFolder
        && outputFile.Filename == $"{TypenameConstants.Contracts}.cs";

    public static string GetContractsOutputPath(
        string? settingsFilePath,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile)
    {
        var root = string.IsNullOrWhiteSpace(settingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settingsFilePath) ?? string.Empty;

        var contractsFolder = Path.GetFullPath(Path.Combine(root, refitGeneratorSettings.ContractsOutputFolder!));
        return Path.Combine(contractsFolder, outputFile.Filename);
    }

    private static bool IsDirectCliGeneration(string? settingsFilePath) =>
        string.IsNullOrWhiteSpace(settingsFilePath);

    private static bool HasExplicitCliOutputOverride(string? cliOutputPath) =>
        !string.IsNullOrWhiteSpace(cliOutputPath) &&
        cliOutputPath != DefaultOutputPath;

    private static string CombineWithSettingsRoot(string root, params string[] segments)
    {
        var combinedPath = Path.Combine(segments);
        return !string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(combinedPath)
            ? Path.Combine(root, combinedPath)
            : combinedPath;
    }
}
