using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal interface IDocumentMerger
{
    OpenApiDocument Merge(OpenApiDocument[] documents);
}
