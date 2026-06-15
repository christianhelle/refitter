namespace Refitter.Core;

/// <summary>
/// Port for running the Refit code generator.
/// Implementations may use the CLI process or call Core directly.
/// </summary>
public interface IGeneratorRunner
{
    /// <summary>
    /// Runs the code generator and returns the list of generated file paths.
    /// </summary>
    /// <param name="settings">The generator settings to use.</param>
    /// <param name="skipValidation">Whether to skip OpenAPI validation.</param>
    /// <param name="noLogging">Whether to suppress logging output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of absolute paths to the generated files.</returns>
    Task<IReadOnlyList<string>> RunAsync(
        RefitGeneratorSettings settings,
        bool skipValidation,
        bool noLogging,
        CancellationToken cancellationToken);
}
