namespace Refitter.Core;

internal static class PathUtilities
{
    public static bool IsHttp(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a reference is an absolute URI with a scheme other than <c>file</c>. Unlike
    /// <see cref="IsHttp"/>, this follows the URI parser, which also reads <c>http:/\host</c> and
    /// <c>http:\\host</c> as network locations.
    /// </summary>
    public static bool TryGetRemoteUri(string reference, out Uri uri)
    {
        var withoutFragment = reference.Split('#')[0];
        if (Uri.TryCreate(withoutFragment, UriKind.Absolute, out Uri? parsed) && !parsed.IsFile)
        {
            uri = parsed;
            return true;
        }

        uri = null!;
        return false;
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

        return basePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
               basePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
    }
}
