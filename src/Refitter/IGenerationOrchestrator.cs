using Refitter.Core;

namespace Refitter;

public interface IGenerationOrchestrator
{
    Task<int> RunAsync(
        RefitGeneratorSettings settings,
        Settings cliSettings,
        IGenerationReporter reporter,
        CancellationToken cancellationToken);
}
