using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Creates instances of <see cref="NSwag.OpenApiDocument"/> from file paths or URLs.
/// Supports loading single documents or merging multiple documents into one.
/// </summary>
[Obsolete("Use RefitGenerator.CreateAsync instead. OpenApiDocumentFactory exposes NSwag types and will be removed in the next major version.")]
public static class OpenApiDocumentFactory
{
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
    public static Task<OpenApiDocument> CreateAsync(
        IEnumerable<string> openApiPaths,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default) =>
        NSwagDocumentFactory.CreateAsync(openApiPaths, allowRemoteReferences, cancellationToken);

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static Task<OpenApiDocument> CreateAsync(
        string openApiPath,
        CancellationToken cancellationToken) =>
        NSwagDocumentFactory.CreateAsync(openApiPath, cancellationToken);

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static Task<OpenApiDocument> CreateAsync(
        string openApiPath,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default) =>
        NSwagDocumentFactory.CreateAsync(openApiPath, allowRemoteReferences, cancellationToken);
}
