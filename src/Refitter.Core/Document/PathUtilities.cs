namespace Refitter.Core;

internal static class PathUtilities
{
    public static bool IsHttp(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsYaml(string path)
    {
        var queryIndex = path.IndexOf('?');
        var fragmentIndex = path.IndexOf('#');
        var endIndex = path.Length;

        if (queryIndex >= 0)
            endIndex = Math.Min(endIndex, queryIndex);
        if (fragmentIndex >= 0)
            endIndex = Math.Min(endIndex, fragmentIndex);

        var basePath = endIndex < path.Length ? path.Substring(0, endIndex) : path;

        return basePath.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
               basePath.EndsWith("yml", StringComparison.OrdinalIgnoreCase);
    }
}
