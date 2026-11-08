namespace Refitter.Core;

/// <summary>
/// Adapts <see cref="OutputPlanner.Plan"/> to the <see cref="IOutputPlanner"/> interface.
/// </summary>
[Obsolete("Use OutputPlanner.Plan. This adapter will be removed in a future version.")]
public class OutputPlannerAdapter : IOutputPlanner
{
    /// <inheritdoc />
    public IReadOnlyList<PlannedFile> Plan(
        GeneratorOutput output,
        IOutputConfiguration config,
        string? settingsFilePath,
        string? cliOutputPath) =>
        OutputPlanner.Plan(output, config, settingsFilePath, cliOutputPath);
}
