
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Creates instances of <see cref="NSwag.OpenApiDocument"/> from file paths or URLs.
/// Supports loading single documents or merging multiple documents into one.
/// </summary>
public static class OpenApiDocumentFactory
{
    private static readonly DocumentMerger DocumentMerger = new DocumentMerger(new DocumentEquivalenceComparer());

    /// <summary>
    /// Creates a merged <see cref="NSwag.OpenApiDocument"/> from multiple paths or URLs.
    /// The first document serves as the base; paths and schemas from subsequent documents are merged in.
    /// </summary>
    /// <param name="openApiPaths">The paths or URLs to the OpenAPI specifications.</param>
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A merged <see cref="NSwag.OpenApiDocument"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openApiPaths"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="openApiPaths"/> is empty.</exception>
    public static async Task<OpenApiDocument> CreateAsync(
        IEnumerable<string> openApiPaths,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        if (openApiPaths == null)
            throw new ArgumentNullException(nameof(openApiPaths));

        var paths = openApiPaths.ToArray();
        if (paths.Length == 0)
            throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));

        var sets = new DocumentSet[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sets[i] = await ReadAsync(paths[i], allowRemoteReferences, cancellationToken).ConfigureAwait(false);
        }

        return await CreateAsync(sets, paths, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses the document sets one by one, and merges them into one document when there is more than one.
    /// </summary>
    internal static Task<OpenApiDocument> CreateAsync(
        IReadOnlyList<DocumentSet> sets,
        CancellationToken cancellationToken = default) =>
        CreateAsync(sets, sets.Select(set => set.RootDocumentPath ?? string.Empty).ToArray(), cancellationToken);

    private static async Task<OpenApiDocument> CreateAsync(
        IReadOnlyList<DocumentSet> sets,
        IReadOnlyList<string> paths,
        CancellationToken cancellationToken)
    {
        if (sets.Count == 0)
            throw new ArgumentException("At least one OpenAPI document must be specified.", nameof(sets));

        var documents = new OpenApiDocument[sets.Count];
        for (var i = 0; i < sets.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            documents[i] = sets[i].RootDocumentPath is null
                ? await OpenApiDocumentParser.ParseAsync(sets[i], cancellationToken).ConfigureAwait(false)
                : await DocumentLoader.ParseAsync(paths[i], sets[i], cancellationToken).ConfigureAwait(false);
        }

        return documents.Length == 1 ? documents[0] : DocumentMerger.Merge(documents);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static Task<OpenApiDocument> CreateAsync(
        string openApiPath,
        CancellationToken cancellationToken) =>
        CreateAsync(openApiPath, allowRemoteReferences: false, cancellationToken);

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static async Task<OpenApiDocument> CreateAsync(
        string openApiPath,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        var set = await ReadAsync(openApiPath, allowRemoteReferences, cancellationToken).ConfigureAwait(false);
        return await CreateAsync(new[] { set }, new[] { openApiPath }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the OpenAPI document at the path or URL, and every file it references, once.
    /// </summary>
    internal static async Task<DocumentSet> ReadAsync(
        string openApiPath,
        bool allowRemoteReferences,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For remote URLs, report a failed download as such before the document is read
        if (PathUtilities.IsHttp(openApiPath))
        {
            string content;
            try
            {
                content = await RemoteDocumentDownloader
                    .DownloadAsync(openApiPath, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiPath}': {ex.Message}", ex);
            }

            return await OpenApiDocumentReader
                .ReadAsync(openApiPath, content, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);
        }

        return await DocumentLoader
            .ReadAsync(openApiPath, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);
    }
}
