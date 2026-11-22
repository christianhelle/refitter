using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Loads a local OpenAPI document with NSwag, and falls back to the multi-file reader for external references
/// NSwag cannot follow.
/// </summary>
internal static class DocumentLoader
{
    public static async Task<OpenApiDocument> LoadAsync(
        string path,
        ReferencePolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(
                "The path parameter cannot be null, empty, or contain only whitespace.",
                nameof(path));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await OpenApiDocumentParser.FromFileAsync(path, policy, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception parserError) when (parserError is not OperationCanceledException)
        {
            // A policy rejection is fatal: the reader must not fetch what the policy refused
            if (ReferenceResolutionException.FindIn(parserError) is { } rejection)
                throw rejection;

            // The parser's error is the one worth reporting: the reader only covers external references
            return await MultiFileDocumentReader.TryReadAsync(path, policy, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException(
                       $"Failed to load OpenAPI document from '{path}': {parserError.Message}",
                       parserError);
        }
    }
}
