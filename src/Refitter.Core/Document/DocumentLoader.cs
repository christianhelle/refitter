using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Parses a local OpenAPI document with NSwag, and falls back to the multi-file reader for external references
/// NSwag cannot follow.
/// </summary>
internal static class DocumentLoader
{
    public static async Task<OpenApiDocument> LoadAsync(
        string path,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(
                "The path parameter cannot be null, empty, or contain only whitespace.",
                nameof(path));

        cancellationToken.ThrowIfCancellationRequested();

        var set = await OpenApiDocumentReader
            .ReadAsync(path, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return await OpenApiDocumentParser.ParseAsync(set, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception parserError) when (parserError is not OperationCanceledException)
        {
            // A rejection is fatal: the reader must not fetch what was refused
            if (ReferenceResolutionException.FindIn(parserError) is { } rejection)
                throw rejection;

            // The parser's error is the one worth reporting: the reader only covers external references
            return await MultiFileDocumentReader.TryReadAsync(path, set, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException(
                       $"Failed to load OpenAPI document from '{path}': {parserError.Message}",
                       parserError);
        }
    }
}
