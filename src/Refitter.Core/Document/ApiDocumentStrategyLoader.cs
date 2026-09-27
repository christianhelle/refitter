namespace Refitter.Core;

internal sealed class ApiDocumentStrategyLoader : IApiDocumentLoader
{
    private readonly IReadOnlyList<IApiDocumentLoadingStrategy> strategies;

    public ApiDocumentStrategyLoader()
        : this(CreateDefaultStrategies())
    {
    }

    public ApiDocumentStrategyLoader(IEnumerable<IApiDocumentLoadingStrategy> strategies)
    {
        if (strategies == null)
            throw new ArgumentNullException(nameof(strategies), "strategies cannot be null");

        this.strategies = strategies.ToList();
    }

    public async Task<ApiDocument> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(
                "The path parameter cannot be null, empty, or contain only whitespace.",
                nameof(path));

        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        foreach (var strategy in strategies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await strategy
                    .TryLoadAsync(path, cancellationToken)
                    .ConfigureAwait(false);

                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException or TaskCanceledException)
                    throw;

                errors.Add($"{strategy.GetType().Name}: {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            $"Failed to load OpenAPI document from '{path}'. " +
            $"All {strategies.Count} strategies failed." +
            (errors.Count > 0
                ? $" Errors: {string.Join("; ", errors)}"
                : ""));
    }

    private static List<IApiDocumentLoadingStrategy> CreateDefaultStrategies()
    {
        return new List<IApiDocumentLoadingStrategy>
        {
            new ApiFileDocumentStrategy(),
            new ApiHttpDocumentStrategy(),
            new ApiOpenApiReaderDocumentStrategy()
        };
    }
}
