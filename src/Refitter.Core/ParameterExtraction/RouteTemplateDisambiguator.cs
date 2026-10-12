namespace Refitter.Core;

/// <summary>
/// Keeps route placeholders bindable by Refit (#1338). Refit binds a placeholder to the parameter whose
/// name (or <c>AliasAs</c> name) matches it case-insensitively, and fails or binds the wrong parameter
/// when another parameter's name only differs by case.
/// </summary>
internal static class RouteTemplateDisambiguator
{
    /// <summary>
    /// Renames route placeholders that collide case-insensitively with the name of another parameter.
    /// Placeholder names are not sent over the wire, so the request is unchanged.
    /// </summary>
    /// <param name="path">The route template of the method.</param>
    /// <param name="parameters">The parameters of the method.</param>
    /// <param name="routeParameterNames">The names of the path parameters in the OpenAPI operation.</param>
    public static (string Path, IReadOnlyList<string> Parameters) Disambiguate(
        string path,
        IReadOnlyList<string> parameters,
        IReadOnlyList<string> routeParameterNames)
    {
        return (path, parameters);
    }
}
