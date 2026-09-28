namespace Refitter.Core;

internal interface IDocumentMerger
{
    ApiDocument Merge(ApiDocument[] documents);
}
