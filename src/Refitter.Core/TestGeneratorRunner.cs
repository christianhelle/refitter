namespace Refitter.Core;

/// <summary>
/// Test implementation of IGeneratorRunner that returns mock file paths.
/// Used for unit testing the MSBuild task without requiring the CLI binary.
/// </summary>
public class TestGeneratorRunner : IGeneratorRunner
{
    private readonly Func<RefitGeneratorSettings, bool, bool, CancellationToken, Task<IReadOnlyList<string>>>? _handler;

    public TestGeneratorRunner()
    {
    }

    public TestGeneratorRunner(
        Func<RefitGeneratorSettings, bool, bool, CancellationToken, Task<IReadOnlyList<string>>> handler)
    {
        _handler = handler;
    }

    public async Task<IReadOnlyList<string>> RunAsync(
        RefitGeneratorSettings settings,
        bool skipValidation,
        bool noLogging,
        CancellationToken cancellationToken)
    {
        if (_handler is not null)
        {
            return await _handler(settings, skipValidation, noLogging, cancellationToken).ConfigureAwait(false);
        }

        return Array.Empty<string>();
    }
}
