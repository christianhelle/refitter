using NSwag;

namespace Refitter.Core;

internal interface IOpenApiDocumentMutator
{
    void Mutate(OpenApiDocument document);
}
