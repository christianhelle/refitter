using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentLoader : IDocumentLoader
{
    private readonly IReadOnlyList<IDocumentLoadingStrategy> strategies;

    public DocumentLoader()
        : this(CreateDefaultStrategies(ReferencePolicy.Unrestricted))
    {
    }

    public DocumentLoader(IEnumerable<IDocumentLoadingStrategy> strategies)
    {
        if (strategies == null)
            throw new ArgumentNullException(nameof(strategies), "strategies cannot be null");

        this.strategies = strategies.ToList();
    }

    public static DocumentLoader Create(ReferencePolicy policy) =>
        new(CreateDefaultStrategies(policy));

    public async Task<OpenApiDocument> LoadAsync(
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
                // A strategy that reports failures explains why it could not load the document
                var result = await (strategy is IFailureReportingStrategy reporting
                        ? reporting.LoadAsync(path, cancellationToken)
                        : strategy.TryLoadAsync(path, cancellationToken))
                    .ConfigureAwait(false);

                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException or TaskCanceledException)
                    throw;

                // A policy rejection is fatal: do not fall back to a strategy that could fetch what was refused
                if (ReferenceResolutionException.FindIn(ex) is { } rejection)
                    throw rejection;

                // Strategies that fall back to the same parser report the same error
                var error = $"{strategy.GetType().Name}: {ex.Message}";
                if (!errors.Any(known => known.EndsWith(": " + ex.Message, StringComparison.Ordinal)))
                    errors.Add(error);
            }
        }

        throw new InvalidOperationException(
            $"Failed to load OpenAPI document from '{path}'. " +
            $"All {strategies.Count} strategies failed." +
            (errors.Count > 0
                ? $" Errors: {string.Join("; ", errors)}"
                : ""));
    }

    private static List<IDocumentLoadingStrategy> CreateDefaultStrategies(ReferencePolicy policy)
    {
        return new List<IDocumentLoadingStrategy>
        {
            new FileDocumentStrategy(policy),
            new OpenApiReaderDocumentStrategy(policy)
        };
    }
}
