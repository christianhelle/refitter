using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.References;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Parses a <see cref="DocumentSet"/> with NSwag. References resolve from the set only, so every file NSwag
/// parses is a file <see cref="OpenApiDocumentReader"/> read and checked.
/// </summary>
internal static class OpenApiDocumentParser
{
    public static Task<OpenApiDocument> ParseAsync(DocumentSet set, CancellationToken cancellationToken = default) =>
        OpenApiDocument.FromJsonAsync(
            set.RootJson,
            set.RootDocumentPath!,
            SchemaType.Swagger2,
            document => new DocumentSetReferenceResolver(
                new OpenApiSchemaResolver(document, new SystemTextJsonSchemaGeneratorSettings()),
                set),
            cancellationToken);

    /// <summary>
    /// Reads <paramref name="content"/> as the OpenAPI document at <paramref name="documentPath"/>, with the
    /// files it references, and parses it.
    /// </summary>
    public static async Task<OpenApiDocument> ParseAsync(
        string content,
        string documentPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        var set = await OpenApiDocumentReader
            .ReadAsync(documentPath, content, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);
        return await ParseAsync(set, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serves referenced files from the set. A file that is not in the set was never checked, so it is
    /// rejected rather than read.
    /// </summary>
    private sealed class DocumentSetReferenceResolver(JsonSchemaAppender schemaAppender, DocumentSet set)
        : JsonReferenceResolver(schemaAppender)
    {
        public override async Task<IJsonReference> ResolveUrlReferenceAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            if (!set.TryGetUrl(url, out var json))
            {
                throw new ReferenceResolutionException(
                    $"Remote '$ref' \"{url}\" was not read with the OpenAPI document and is rejected.");
            }

            // Relative references in a remote document resolve against its URL, which is its document path
            return await JsonSchema.FromJsonAsync(json, url, _ => this, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IJsonReference> ResolveFileReferenceAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var fullPath = Path.GetFullPath(filePath);
            if (set.TryGetFile(fullPath, out var json))
                return await JsonSchema.FromJsonAsync(json, fullPath, _ => this, cancellationToken).ConfigureAwait(false);

            // A missing file is an ordinary error; a file that exists but was not read is a path the reader
            // resolved differently, and is never read unchecked
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Could not find file '{fullPath}'.", fullPath);

            throw new ReferenceResolutionException(
                $"Local '$ref' to \"{fullPath}\" was not read with the OpenAPI document and is rejected.");
        }
    }
}
