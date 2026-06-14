using System.Text.RegularExpressions;
using NSwag;

namespace Refitter.Core;

public static class RefitDocumentFilter
{
    public static OpenApiDocument FilterByTags(OpenApiDocument document, string[] includeTags)
    {
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

    public static OpenApiDocument FilterByPath(OpenApiDocument document, string[] includePathMatches)
    {
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
