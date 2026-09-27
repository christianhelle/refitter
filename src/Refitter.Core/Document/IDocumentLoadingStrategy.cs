namespace Refitter.Core;

internal interface IDocumentLoadingStrategy
{
    Task<ApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default);
}
