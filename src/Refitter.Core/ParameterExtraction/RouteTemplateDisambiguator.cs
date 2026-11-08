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
    public static (string Path, IReadOnlyList<MethodParameter> Parameters) Disambiguate(
        string path,
        IReadOnlyList<MethodParameter> parameters,
        IReadOnlyList<string> routeParameterNames)
    {
        if (string.IsNullOrEmpty(path))
            return (path, parameters);

        var result = parameters.ToList();
        var placeholders = routeParameterNames
            .Where(name => path.Contains($"{{{name}}}"))
            .ToList();

        for (var placeholderIndex = 0; placeholderIndex < placeholders.Count; placeholderIndex++)
        {
            var placeholder = placeholders[placeholderIndex];

            // Route parameters are extracted first and keep their name when deduplicated,
            // so the first parameter bound to the placeholder is the route parameter
            var routeIndex = result.FindIndex(parameter => parameter.BoundName == placeholder);
            if (routeIndex < 0)
                continue;

            var otherParameters = result.Where((_, index) => index != routeIndex).ToList();
            var otherPlaceholders = placeholders.Where((_, index) => index != placeholderIndex).ToList();

            var collidingNames = new HashSet<string>(
                otherParameters.Select(parameter => parameter.BoundName).Concat(otherPlaceholders),
                StringComparer.OrdinalIgnoreCase);
            if (!collidingNames.Contains(placeholder))
                continue;

            var usedNames = new HashSet<string>(
                collidingNames.Concat(otherParameters.Select(parameter => parameter.Identifier)),
                StringComparer.OrdinalIgnoreCase);

            var name = result[routeIndex].Identifier;
            var uniqueName = name;
            var suffix = 2;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = name + suffix++;
            }

            path = path.Replace($"{{{placeholder}}}", $"{{{uniqueName}}}");
            placeholders[placeholderIndex] = uniqueName;
            result[routeIndex] = WithAlias(result[routeIndex], uniqueName);
        }

        return (path, result);
    }

    // Binds the parameter to the alias, which is redundant when it equals the parameter name
    private static MethodParameter WithAlias(MethodParameter parameter, string alias)
    {
        // A parameter without an alias is bound by its name, which collides, so its alias is never redundant
        if (parameter.Alias is null)
            return parameter with { Alias = alias, IsAliasSeparate = true };

        return alias == parameter.Identifier
            ? parameter with { Alias = null, IsAliasSeparate = false }
            : parameter with { Alias = alias };
    }
}
