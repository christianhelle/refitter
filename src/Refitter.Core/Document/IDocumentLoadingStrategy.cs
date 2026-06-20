using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal interface IDocumentLoadingStrategy
{
    Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default);
}
