using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class FileDocumentStrategy : IFailureReportingStrategy
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

    public async Task<OpenApiDocument?> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (PathUtilities.IsHttp(path))
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        return await OpenApiDocumentParser.FromFileAsync(path, policy, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;

            // A policy rejection is fatal: do not let another strategy retry what the policy refused
            if (ReferenceResolutionException.FindIn(ex) is { } rejection)
                throw rejection;

            return null;
        }
    }
}
