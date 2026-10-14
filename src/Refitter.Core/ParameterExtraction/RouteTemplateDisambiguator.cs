using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refitter.Core;

/// <summary>
/// Keeps route placeholders bindable by Refit (#1338). Refit binds a placeholder to the parameter whose
/// name (or <c>AliasAs</c> name) matches it case-insensitively, and fails or binds the wrong parameter
/// when another parameter's name only differs by case.
/// </summary>
internal static class RouteTemplateDisambiguator
{
    private const string Prefix = "interface I { void M(";

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
        if (string.IsNullOrEmpty(path))
            return (path, parameters);

        var result = parameters.ToList();
        var placeholders = routeParameterNames
            .Where(name => path.Contains($"{{{name}}}"))
            .ToList();

        for (var placeholderIndex = 0; placeholderIndex < placeholders.Count; placeholderIndex++)
        {
            var placeholder = placeholders[placeholderIndex];
            var parsed = result.Select(Parse).ToList();

            // Route parameters are extracted first and keep their name when deduplicated,
            // so the first parameter bound to the placeholder is the route parameter
            var routeIndex = parsed.FindIndex(parameter => GetUrlName(parameter.Syntax) == placeholder);
            if (routeIndex < 0)
                continue;

            var otherParameters = parsed.Where((_, index) => index != routeIndex).ToList();
            var otherPlaceholders = placeholders.Where((_, index) => index != placeholderIndex).ToList();

            var collidingNames = new HashSet<string>(
                otherParameters.Select(parameter => GetUrlName(parameter.Syntax)).Concat(otherPlaceholders),
                StringComparer.OrdinalIgnoreCase);
            if (!collidingNames.Contains(placeholder))
                continue;

            var usedNames = new HashSet<string>(
                collidingNames.Concat(otherParameters.Select(parameter => parameter.Syntax.Identifier.ValueText)),
                StringComparer.OrdinalIgnoreCase);

            var name = parsed[routeIndex].Syntax.Identifier.ValueText;
            var uniqueName = name;
            var suffix = 2;
            while (usedNames.Contains(uniqueName))
            {
                uniqueName = name + suffix++;
            }

            path = path.Replace($"{{{placeholder}}}", $"{{{uniqueName}}}");
            placeholders[placeholderIndex] = uniqueName;
            result[routeIndex] = WithAlias(parsed[routeIndex], uniqueName);
        }

        return (path, result);
    }

    private static (string Text, ParameterSyntax Syntax) Parse(string parameter)
    {
        var tree = CSharpSyntaxTree.ParseText($"{Prefix}{parameter}); }}");
        return (parameter, tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First());
    }

    // The name Refit binds the parameter by
    private static string GetUrlName(ParameterSyntax parameter) =>
        GetAliasAttribute(parameter)?.ArgumentList?.Arguments[0].Expression is LiteralExpressionSyntax literal
            ? literal.Token.ValueText
            : parameter.Identifier.ValueText;

    private static AttributeSyntax? GetAliasAttribute(ParameterSyntax parameter) =>
        parameter.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attribute => attribute.Name.ToString() == "AliasAs");

    // Binds the parameter to the alias, which is redundant when it equals the parameter name
    private static string WithAlias((string Text, ParameterSyntax Syntax) parameter, string alias)
    {
        // A parameter without an alias is bound by its name, which collides, so its alias is never redundant
        var aliasAttribute = GetAliasAttribute(parameter.Syntax);
        if (aliasAttribute == null)
            return $"[AliasAs(\"{alias}\")] {parameter.Text}";

        var isRedundant = alias == parameter.Syntax.Identifier.ValueText;

        var attributeList = parameter.Syntax.AttributeLists.First(list => list.Attributes.Contains(aliasAttribute));
        var attributes = attributeList.Attributes
            .Where(attribute => attribute != aliasAttribute || !isRedundant)
            .Select(attribute => attribute == aliasAttribute ? $"AliasAs(\"{alias}\")" : attribute.ToString())
            .ToList();

        // Removing the only attribute of a list also removes the whitespace after it
        var start = attributeList.Span.Start - Prefix.Length;
        var end = (attributes.Count == 0 ? attributeList.FullSpan.End : attributeList.Span.End) - Prefix.Length;
        var replacement = attributes.Count == 0 ? string.Empty : $"[{string.Join(", ", attributes)}]";
        return parameter.Text.Substring(0, start) + replacement + parameter.Text.Substring(end);
    }
}
