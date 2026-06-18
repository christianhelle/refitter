using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Creates instances of <see cref="NSwag.OpenApiDocument"/> from file paths or URLs.
/// Supports loading single documents or merging multiple documents into one.
/// </summary>
public static class OpenApiDocumentFactory
{
    private static readonly IDocumentLoader DocumentLoader = new DocumentLoader();
    private static readonly IDocumentMerger DocumentMerger = new DocumentMerger(new DocumentEquivalenceComparer());

    /// <summary>
    /// Creates a merged <see cref="NSwag.OpenApiDocument"/> from multiple paths or URLs.
    /// The first document serves as the base; paths and schemas from subsequent documents are merged in.
    /// </summary>
    /// <param name="openApiPaths">The paths or URLs to the OpenAPI specifications.</param>
    /// <returns>A merged <see cref="NSwag.OpenApiDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openApiPaths"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="openApiPaths"/> is empty.</exception>
    public static async Task<OpenApiDocument> CreateAsync(IEnumerable<string> openApiPaths)
    {
        if (openApiPaths == null)
            throw new ArgumentNullException(nameof(openApiPaths));

        var paths = openApiPaths.ToArray();
        if (paths.Length == 0)
            throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));

        if (paths.Length == 1)
            return await CreateAsync(paths[0]).ConfigureAwait(false);

        var documents = new OpenApiDocument[paths.Length];
        for (var i = 0; i < paths.Length; i++)
            documents[i] = await DocumentLoader.LoadAsync(paths[i]).ConfigureAwait(false);

        return DocumentMerger.Merge(documents);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static async Task<OpenApiDocument> CreateAsync(string openApiPath)
    {
        return await DocumentLoader.LoadAsync(openApiPath).ConfigureAwait(false);
    }
}
