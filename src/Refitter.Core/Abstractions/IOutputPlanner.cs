using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Resolves a <see cref="GeneratorOutput"/> into a list of <see cref="PlannedFile"/> instances.
/// Pure logic — no I/O.
/// </summary>
public interface IOutputPlanner
{
    /// <summary>
    /// Plans the output file paths for the given generator output.
    /// </summary>
    /// <param name="output">The generator output containing generated files.</param>
    /// <param name="config">The output configuration.</param>
    /// <param name="settingsFilePath">
    /// The path to the settings file, or <c>null</c> for direct CLI generation.
    /// </param>
    /// <param name="cliOutputPath">
    /// The output path specified via CLI, or <c>null</c>.
    /// </param>
    /// <returns>A read-only list of planned files with resolved paths and content.</returns>
    IReadOnlyList<PlannedFile> Plan(
        GeneratorOutput output,
        IOutputConfiguration config,
        string? settingsFilePath,
        string? cliOutputPath);
}
