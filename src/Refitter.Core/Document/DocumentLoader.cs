using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Reads a local OpenAPI document with the files it references, and parses it with NSwag.
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
            // A rejection is fatal: it is reported as it is
            if (ReferenceResolutionException.FindIn(parserError) is { } rejection)
                throw rejection;

            throw new InvalidOperationException(
                $"Failed to load OpenAPI document from '{path}': {parserError.Message}",
                parserError);
        }
    }
}
