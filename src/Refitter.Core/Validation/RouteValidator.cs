namespace Refitter.Core.Validation;

/// <summary>
/// Rejects routes that Refit would resolve as network-path references. A route starting with two
/// slashes (or a slash and a backslash, which .NET treats the same way for http) replaces the host of
/// the configured base address, so the request and its Authorization header go to another host.
/// See GHSA-7hgj-q592-v892.
/// </summary>
internal static class RouteValidator
{
    internal static bool IsRelativeRoute(string? route) =>
        !string.IsNullOrEmpty(route)
        && route![0] == '/'
        && (route.Length == 1 || (route[1] != '/' && route[1] != '\\'));

    internal static void EnsureRelativeRoute(string? route)
    {
        if (!IsRelativeRoute(route))
        {
            throw new InvalidOperationException(
                $"OpenAPI path '{route}' is rejected: it must start with a single '/' because a path starting " +
                "with two slashes or a backslash would be resolved as a different host.");
        }
    }
}
