namespace Refitter.Core;

/// <summary>
/// Pure output-path planning utilities for the Refit generator.
/// </summary>
public static class OutputPlanner
{
    /// <summary>
    /// Determines whether an output file should be rerouted to the contracts output folder.
    /// </summary>
    public static bool ShouldRerouteToContractsFolder(
        RefitGeneratorSettings settings,
        GeneratedCode outputFile) =>
        !string.IsNullOrWhiteSpace(settings.ContractsOutputFolder)
        && settings.ContractsOutputFolder != RefitGeneratorSettings.DefaultOutputFolder
        && outputFile.Filename == $"{TypenameConstants.Contracts}.cs";
}
