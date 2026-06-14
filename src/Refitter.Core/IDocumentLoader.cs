using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal interface IDocumentLoader
{
    Task<OpenApiDocument> LoadAsync(string path);
}
