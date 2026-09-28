using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Filters an OpenAPI document by tags and path patterns.
/// Each filter operation returns a new document without mutating the input.
/// </summary>
internal static class ApiDocumentFilter
{
    /// <summary>
    /// Removes operations from the document that do not match any of the specified tags.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includeTags">Tags to include. When empty, all operations are kept.</param>
    /// <returns>A new OpenAPI document with only matching operations.</returns>
    public static ApiDocument FilterByTags(ApiDocument document, string[] includeTags)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (includeTags == null) throw new ArgumentNullException(nameof(includeTags));

        if (includeTags.Length == 0)
            return document;

        var result = CloneDocument(document);
        var clonedPaths = result.Paths
            .Where(pair => pair.Value != null)
            .ToArray();

        foreach (var path in clonedPaths)
            RemoveOperationsWithoutTags(result, path.Key, path.Value, includeTags);

        return result;
    }

    /// <summary>Removes the operations of a path that have none of the tags, and the path when it has none left.</summary>
    private static void RemoveOperationsWithoutTags(ApiDocument document, string path, ApiPathItem pathItem, string[] includeTags)
    {
        var methods = pathItem
            .Where(pair => pair.Value != null)
            .ToArray();

        foreach (var method in methods)
        {
            var exclude = method.Value.Tags?.Exists(includeTags.Contains) != true;
            if (exclude)
                pathItem.Remove(method.Key);

            if (pathItem.Count == 0)
                document.Paths.Remove(path);
        }
    }

    /// <summary>
    /// Removes paths from the document that do not match any of the specified regular expressions.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includePathMatches">Regular expressions to match paths against. When empty, all paths are kept.</param>
    /// <returns>A new OpenAPI document with only matching paths.</returns>
    public static ApiDocument FilterByPath(ApiDocument document, string[] includePathMatches)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (includePathMatches == null) throw new ArgumentNullException(nameof(includePathMatches));

        if (includePathMatches.Length == 0)
            return document;

        var result = CloneDocument(document);
        var regexes = includePathMatches
            .Select(x => new Regex(x, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
            .ToArray();
        var paths = result.Paths.Keys
            .Where(pathKey => regexes.All(t => !t.IsMatch(pathKey)))
            .ToArray();

        foreach (string pathKey in paths)
            result.Paths.Remove(pathKey);

        return result;
    }

    private static ApiDocument CloneDocument(ApiDocument document)
        => ApiDocumentWriter.Clone(document);
}
