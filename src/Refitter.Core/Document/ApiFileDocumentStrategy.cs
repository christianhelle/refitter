namespace Refitter.Core;

internal sealed class ApiFileDocumentStrategy : IApiDocumentLoadingStrategy
{
    public Task<ApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (PathUtilities.IsHttp(path))
            return Task.FromResult<ApiDocument?>(null);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<ApiDocument?>(ApiDocumentLoader.LoadFile(path));
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;

            return Task.FromResult<ApiDocument?>(null);
        }
    }
}
