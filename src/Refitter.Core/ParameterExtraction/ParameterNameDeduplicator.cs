namespace Refitter.Core;

/// <summary>
/// Makes the parameter names of a generated method unique (#1269). Parameters come from different sources
/// (path, query, header, body, multipart form, cancellation token), and names such as <c>id</c> or
/// <c>cancellationToken</c> can occur in more than one of them.
/// </summary>
internal static class ParameterNameDeduplicator
{
    /// <summary>
    /// Renames later parameters that reuse an earlier name by adding a numeric suffix. A renamed parameter
    /// that Refit binds by name (route, query or multipart) gets an <c>AliasAs</c> attribute with its
    /// original name, so the request is unchanged.
    /// </summary>
    /// <param name="parameters">The parameters of the method.</param>
    /// <param name="hasAppendedParameter">
    /// Whether the last parameter is the cancellation token or request options argument Refitter appends,
    /// which keeps its well-known name.
    /// </param>
    public static IReadOnlyList<MethodParameter> Deduplicate(
        IReadOnlyList<MethodParameter> parameters,
        bool hasAppendedParameter)
    {
        var appendedIndex = hasAppendedParameter ? parameters.Count - 1 : -1;

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        if (appendedIndex >= 0)
            usedNames.Add(parameters[appendedIndex].Identifier);

        var result = new List<MethodParameter>(parameters.Count);
        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            var name = parameter.Identifier;
            if (index == appendedIndex || usedNames.Add(name))
            {
                result.Add(parameter);
                continue;
            }

            var uniqueName = name;
            var suffix = 2;
            while (!usedNames.Add(uniqueName))
            {
                uniqueName = name + suffix++;
            }

            result.Add(parameter.IsBoundByName
                ? parameter with { Name = uniqueName, Alias = parameter.WireName, IsAliasSeparate = true }
                : parameter with { Name = uniqueName });
        }

        return result;
    }
}
