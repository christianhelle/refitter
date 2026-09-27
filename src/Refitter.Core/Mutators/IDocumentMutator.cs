namespace Refitter.Core;

/// <summary>Changes a document before code is generated from it.</summary>
internal interface IDocumentMutator
{
    void Mutate(ApiDocument document);
}
