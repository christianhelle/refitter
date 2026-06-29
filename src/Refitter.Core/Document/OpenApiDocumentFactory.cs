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

        if (paths.Length == 1)
            return await CreateAsync(paths[0], allowRemoteReferences, cancellationToken).ConfigureAwait(false);

        var documents = new OpenApiDocument[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For remote URLs, fetch once and reuse content for both validation and parsing
            if (PathUtilities.IsHttp(paths[i]))
            {
                string content;
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                    content = await client.GetStringAsync(paths[i], cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to download OpenAPI document from '{paths[i]}'.", ex);
                }

                await ReferenceGuard.ValidateAsync(paths[i], content, allowRemoteReferences, cancellationToken).ConfigureAwait(false);

                documents[i] = PathUtilities.IsYaml(paths[i])
                    ? await NSwag.OpenApiYamlDocument.FromYamlAsync(content, cancellationToken).ConfigureAwait(false)
                    : await OpenApiDocument.FromJsonAsync(content, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ReferenceGuard.ValidateAsync(paths[i], allowRemoteReferences, cancellationToken).ConfigureAwait(false);
                documents[i] = await DocumentLoader.LoadAsync(paths[i], cancellationToken).ConfigureAwait(false);
            }
        }

        return DocumentMerger.Merge(documents);
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
        // For remote URLs, fetch once and reuse content for both validation and parsing
        if (PathUtilities.IsHttp(openApiPath))
        {
            string content;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                content = await client.GetStringAsync(openApiPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiPath}'.", ex);
            }

            await ReferenceGuard.ValidateAsync(openApiPath, content, allowRemoteReferences, cancellationToken).ConfigureAwait(false);

            return PathUtilities.IsYaml(openApiPath)
                ? await NSwag.OpenApiYamlDocument.FromYamlAsync(content, cancellationToken).ConfigureAwait(false)
                : await OpenApiDocument.FromJsonAsync(content, cancellationToken).ConfigureAwait(false);
        }

        await ReferenceGuard.ValidateAsync(openApiPath, allowRemoteReferences, cancellationToken).ConfigureAwait(false);
        return await DocumentLoader.LoadAsync(openApiPath, cancellationToken).ConfigureAwait(false);
    }
}
