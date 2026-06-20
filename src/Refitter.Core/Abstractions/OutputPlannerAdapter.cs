using System.Collections.Generic;

namespace Refitter.Core;

/// <summary>
/// Adapts the static <see cref="OutputPlanner"/> methods to the <see cref="IOutputPlanner"/> interface.
/// </summary>
public class OutputPlannerAdapter : IOutputPlanner
{
    /// <inheritdoc />
    public IReadOnlyList<PlannedFile> Plan(
        GeneratorOutput output,
        IOutputConfiguration config,
        string? settingsFilePath,
        string? cliOutputPath)
    {
        if (config.GenerateMultipleFiles)
        {
            return OutputPlanner.PlanMultipleFiles(
                settingsFilePath,
                cliOutputPath,
                config,
                output);
        }

        var code = output.Files.Count > 0
            ? output.Files[0].Content
            : string.Empty;

        return
        [
            OutputPlanner.PlanSingleFile(
                settingsFilePath,
                cliOutputPath,
                config,
                code)
        ];
    }
}
