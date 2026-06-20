using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentLoader : IDocumentLoader
{
    private readonly IReadOnlyList<IDocumentLoadingStrategy> strategies;

    public DocumentLoader()
        : this(CreateDefaultStrategies())
    {
    }

    public DocumentLoader(IEnumerable<IDocumentLoadingStrategy> strategies)
    {
        this.strategies = strategies.ToList();
    }

    public async Task<OpenApiDocument> LoadAsync(
        string openApiPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(openApiPath))
            throw new ArgumentException(
                "The openApiPath parameter cannot be null, empty, or contain only whitespace.",
                nameof(openApiPath));

        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        foreach (var strategy in strategies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await strategy
                    .TryLoadAsync(openApiPath, cancellationToken)
                    .ConfigureAwait(false);

                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {
                errors.Add($"{strategy.GetType().Name}: {ex.Message}");
            }
        }

        throw new InvalidOperationException(
            $"Failed to load OpenAPI document from '{openApiPath}'. " +
            $"All {strategies.Count} strategies failed." +
            (errors.Count > 0
                ? $" Errors: {string.Join("; ", errors)}"
                : ""));
    }

    private static List<IDocumentLoadingStrategy> CreateDefaultStrategies()
    {
        return new List<IDocumentLoadingStrategy>
        {
            new FileDocumentStrategy(),
            new HttpDocumentStrategy(),
            new OpenApiReaderDocumentStrategy()
        };
    }
}
