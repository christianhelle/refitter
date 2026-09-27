namespace Refitter.Core;

internal interface IApiDocumentLoadingStrategy
{
    Task<ApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default);
}
