using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// A loading strategy that can report why it failed. <see cref="IDocumentLoadingStrategy.TryLoadAsync"/> turns
/// every failure into <c>null</c>, which leaves nothing to tell the user, so <see cref="DocumentLoader"/> calls
/// this instead and collects the exceptions.
/// </summary>
internal interface IFailureReportingStrategy : IDocumentLoadingStrategy
{
    /// <summary>
    /// Loads the document. Returns <c>null</c> when the strategy does not apply to the path, and throws when it
    /// applies but the document cannot be loaded.
    /// </summary>
    Task<OpenApiDocument?> LoadAsync(string path, CancellationToken cancellationToken = default);
}
