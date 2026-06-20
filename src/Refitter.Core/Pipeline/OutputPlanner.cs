namespace Refitter.Core;

/// <summary>
/// Represents a planned output file with its relative path and generated content.
/// </summary>
/// <param name="Path">The relative file path for the output.</param>
/// <param name="Content">The generated code content.</param>
public sealed record PlannedFile(string Path, string Content);

/// <summary>
/// Plans the output file paths for generated Refit code.
/// Supports single-file and multi-file output modes, CLI and settings-file driven generation,
/// and optional rerouting of contracts to a separate output folder.
/// </summary>
public static class OutputPlanner
{
    /// <summary>
    /// The default output file name used when no explicit output path is specified.
    /// </summary>
    public const string DefaultOutputPath = "Output.cs";

    /// <summary>
    /// Plans a single output file, determining its path based on the generation mode.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file, or <c>null</c> for direct CLI generation.</param>
    /// <param name="cliOutputPath">The output path specified via CLI, or <c>null</c>.</param>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <param name="code">The generated code content.</param>
    /// <returns>A <see cref="PlannedFile"/> with the resolved output path and content.</returns>
    public static PlannedFile PlanSingleFile(
        string? settingsFilePath,
        string? cliOutputPath,
        IOutputConfiguration outputConfiguration,
        string code) =>
        new(GetSingleFileOutputPath(settingsFilePath, cliOutputPath, outputConfiguration), code);

    /// <summary>
    /// Plans multiple output files, determining each file's path based on the generation mode
    /// and whether the file should be rerouted to the contracts output folder.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file, or <c>null</c> for direct CLI generation.</param>
    /// <param name="cliOutputPath">The output path specified via CLI, or <c>null</c>.</param>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <param name="generatorOutput">The generator output containing multiple files.</param>
    /// <returns>A read-only list of <see cref="PlannedFile"/> with resolved output paths and content.</returns>
    public static IReadOnlyList<PlannedFile> PlanMultipleFiles(
        string? settingsFilePath,
        string? cliOutputPath,
        IOutputConfiguration outputConfiguration,
        GeneratorOutput generatorOutput)
    {
        var planned = new List<PlannedFile>(generatorOutput.Files.Count);
        foreach (var outputFile in generatorOutput.Files)
        {
            var path = ShouldRerouteToContractsFolder(outputConfiguration, outputFile)
                ? GetContractsOutputPath(settingsFilePath, outputConfiguration, outputFile)
                : GetMultiFileOutputPath(settingsFilePath, cliOutputPath, outputConfiguration, outputFile);

            planned.Add(new(path, outputFile.Content));
        }

        return planned;
    }

    /// <summary>
    /// Resolves the output path for single-file generation, accounting for settings file location,
    /// CLI overrides, and configured output folder/filename in the settings.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file, or <c>null</c> for direct CLI generation.</param>
    /// <param name="cliOutputPath">The output path specified via CLI, or <c>null</c>.</param>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <returns>The resolved output file path.</returns>
    public static string GetSingleFileOutputPath(
        string? settingsFilePath,
        string? cliOutputPath,
        IOutputConfiguration outputConfiguration)
    {
        if (IsDirectCliGeneration(settingsFilePath))
        {
            return HasExplicitCliOutputOverride(cliOutputPath)
                ? cliOutputPath!
                : DefaultOutputPath;
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
            var filename = outputConfiguration.OutputFilename ?? "Output.cs";
            outputPath = !string.IsNullOrWhiteSpace(outputConfiguration.OutputFolder)
                ? Path.Combine(outputConfiguration.OutputFolder, filename)
                : filename;
        }

        if (!string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(outputPath))
            outputPath = Path.Combine(root, outputPath);

        return outputPath;
    }

    /// <summary>
    /// Resolves the output path for a single file in multi-file generation mode,
    /// accounting for settings file location, CLI overrides, and configured output folder.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file, or <c>null</c> for direct CLI generation.</param>
    /// <param name="cliOutputPath">The output path specified via CLI, or <c>null</c>.</param>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <param name="outputFile">The generated code file to produce a path for.</param>
    /// <returns>The resolved output file path.</returns>
    public static string GetMultiFileOutputPath(
        string? settingsFilePath,
        string? cliOutputPath,
        IOutputConfiguration outputConfiguration,
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
            : outputConfiguration.OutputFolder;

        return !string.IsNullOrWhiteSpace(outputFolder)
            ? CombineWithSettingsRoot(root, outputFolder!, outputFile.Filename)
            : CombineWithSettingsRoot(root, outputFile.Filename);
    }

    /// <summary>
    /// Determines whether the specified output file should be rerouted to the contracts output folder.
    /// Rerouting occurs when <see cref="IOutputConfiguration.ContractsOutputFolder"/> differs from
    /// the primary <see cref="IOutputConfiguration.OutputFolder"/> and the file is the contract's file.
    /// </summary>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <param name="outputFile">The generated code file to evaluate.</param>
    /// <returns><c>true</c> if the file should be rerouted to the contracts folder; otherwise, <c>false</c>.</returns>
    public static bool ShouldRerouteToContractsFolder(
        IOutputConfiguration outputConfiguration,
        GeneratedCode outputFile) =>
        !string.IsNullOrWhiteSpace(outputConfiguration.ContractsOutputFolder)
     && outputConfiguration.ContractsOutputFolder != outputConfiguration.OutputFolder
     && outputFile.Filename == $"{TypenameConstants.Contracts}.cs";

    /// <summary>
    /// Resolves the output path for a contract's file, placing it in the configured contracts output folder
    /// relative to the settings file location.
    /// </summary>
    /// <param name="settingsFilePath">The path to the settings file, or <c>null</c> for direct CLI generation.</param>
    /// <param name="outputConfiguration">The output configuration.</param>
    /// <param name="outputFile">The generated contracts code file.</param>
    /// <returns>The resolved contracts output filepath.</returns>
    public static string GetContractsOutputPath(
        string? settingsFilePath,
        IOutputConfiguration outputConfiguration,
        GeneratedCode outputFile)
    {
        var root = string.IsNullOrWhiteSpace(settingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settingsFilePath) ?? string.Empty;

        return Path.Combine(
            Path.GetFullPath(Path.Combine(root, outputConfiguration.ContractsOutputFolder!)),
            outputFile.Filename);
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
