using NSwag;

namespace Refitter.Core;

/// <summary>
/// Filters an OpenAPI document by tags and path patterns.
/// Each filter operation returns a new document without mutating the input.
/// </summary>
[Obsolete("Use RefitGeneratorSettings.IncludeTags and IncludePathMatches instead. RefitDocumentFilter exposes NSwag types and will be removed in the next major version.")]
public static class RefitDocumentFilter
{
    /// <summary>
    /// Removes operations from the document that do not match any of the specified tags.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includeTags">Tags to include. When empty, all operations are kept.</param>
    /// <returns>A new OpenAPI document with only matching operations.</returns>
    public static OpenApiDocument FilterByTags(OpenApiDocument document, string[] includeTags) =>
        NSwagDocumentFilter.FilterByTags(document, includeTags);

    /// <summary>
    /// Removes paths from the document that do not match any of the specified regular expressions.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includePathMatches">Regular expressions to match paths against. When empty, all paths are kept.</param>
    /// <returns>A new OpenAPI document with only matching paths.</returns>
    public static OpenApiDocument FilterByPath(OpenApiDocument document, string[] includePathMatches) =>
        NSwagDocumentFilter.FilterByPath(document, includePathMatches);
}
