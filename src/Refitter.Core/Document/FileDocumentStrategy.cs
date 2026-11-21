using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class FileDocumentStrategy : IDocumentLoadingStrategy
{
    private readonly ReferencePolicy policy;

    public FileDocumentStrategy()
        : this(ReferencePolicy.Unrestricted)
    {
    }

    public FileDocumentStrategy(ReferencePolicy policy)
    {
        this.policy = policy;
    }

    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (PathUtilities.IsHttp(path))
            return null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await OpenApiDocumentParser.FromFileAsync(path, policy, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;

            return null;
        }
    }
}
