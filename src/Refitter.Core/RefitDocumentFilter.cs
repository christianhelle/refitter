using System.Text.RegularExpressions;

using NSwag;

namespace Refitter.Core;

/// <summary>
/// Filters an OpenAPI document by tags and path patterns.
/// Each filter operation returns a new document without mutating the input.
/// </summary>
public static class RefitDocumentFilter
{
    /// <summary>
    /// Removes operations from the document that do not match any of the specified tags.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includeTags">Tags to include. When empty, all operations are kept.</param>
    /// <returns>A new OpenAPI document with only matching operations.</returns>
    public static OpenApiDocument FilterByTags(OpenApiDocument document, string[] includeTags)
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
        {
            if (path.Value == null)
                continue;

            var methods = path.Value
                .Where(pair => pair.Value != null)
                .ToArray();

            foreach (var method in methods)
            {
                if (method.Value == null)
                    continue;

                var exclude = method.Value.Tags?.Exists(includeTags.Contains) != true;
                if (exclude)
                    path.Value.Remove(method.Key);

                if (path.Value.Count == 0)
                    result.Paths.Remove(path.Key);
            }
        }

        return result;
    }

    /// <summary>
    /// Removes paths from the document that do not match any of the specified regular expressions.
    /// Returns a new document; the original is not modified.
    /// </summary>
    /// <param name="document">The OpenAPI document to filter.</param>
    /// <param name="includePathMatches">Regular expressions to match paths against. When empty, all paths are kept.</param>
    /// <returns>A new OpenAPI document with only matching paths.</returns>
    public static OpenApiDocument FilterByPath(OpenApiDocument document, string[] includePathMatches)
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
            .Where(pathKey =>
            {
                for (var i = 0; i < regexes.Length; i++)
                {
                    if (regexes[i].IsMatch(pathKey))
                        return false;
                }

                return true;
            })
            .ToArray();

        foreach (string pathKey in paths)
            result.Paths.Remove(pathKey);

        return result;
    }

    private static OpenApiDocument CloneDocument(OpenApiDocument document)
        => OpenApiDocument.FromJsonAsync(document.ToJson()).GetAwaiter().GetResult();
}
