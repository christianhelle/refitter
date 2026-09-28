namespace Refitter.Core;

internal interface IDocumentLoader
{
    Task<ApiDocument> LoadAsync(string path, CancellationToken cancellationToken = default);
}
