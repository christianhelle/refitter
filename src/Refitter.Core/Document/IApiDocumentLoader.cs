namespace Refitter.Core;

internal interface IApiDocumentLoader
{
    Task<ApiDocument> LoadAsync(string path, CancellationToken cancellationToken = default);
}
