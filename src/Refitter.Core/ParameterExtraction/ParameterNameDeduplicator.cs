using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refitter.Core;

/// <summary>
/// Makes the parameter names of a generated method unique (#1269). Parameters come from different sources
/// (path, query, header, body, multipart form, cancellation token), and names such as <c>id</c> or
/// <c>cancellationToken</c> can occur in more than one of them.
/// </summary>
internal static class ParameterNameDeduplicator
{
    private const string Prefix = "interface I { void M(";

    // Parameters whose wire name does not come from the parameter name
    private static readonly HashSet<string> NameIndependentAttributes = new(StringComparer.Ordinal)
    {
        "AliasAs",
        "Header",
        "Body",
    };

    // Parameters Refitter always appends, which keep their well-known names
    private static readonly HashSet<string> ReservedParameterTypes = new(StringComparer.Ordinal)
    {
        "CancellationToken",
        "IApizrRequestOptions",
    };

    /// <summary>
    /// Renames later parameters that reuse an earlier name by adding a numeric suffix. A renamed parameter
    /// that Refit binds by name (route, query or multipart) gets an <c>AliasAs</c> attribute with its
    /// original name, so the request is unchanged.
    /// </summary>
    public static IReadOnlyList<string> Deduplicate(IReadOnlyList<string> parameters)
    {
        var parsed = parameters.Select(Parse).ToList();

        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in parsed.Where(p => ReservedParameterTypes.Contains(p.Syntax.Type!.ToString())))
            usedNames.Add(parameter.Syntax.Identifier.ValueText);

        var result = new List<string>(parameters.Count);
        foreach (var parameter in parsed)
        {
            var name = parameter.Syntax.Identifier.ValueText;
            if (ReservedParameterTypes.Contains(parameter.Syntax.Type!.ToString()) || usedNames.Add(name))
            {
                result.Add(parameter.Text);
                continue;
            }

            var uniqueName = name;
            for (var suffix = 2; !usedNames.Add(uniqueName); suffix++)
            {
                uniqueName = name + suffix;
            }

            result.Add(Rename(parameter, name, uniqueName));
        }

        return result;
    }

    private static (string Text, ParameterSyntax Syntax) Parse(string parameter)
    {
        var tree = CSharpSyntaxTree.ParseText($"{Prefix}{parameter}); }}");
        return (parameter, tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First());
    }

    private static string Rename((string Text, ParameterSyntax Syntax) parameter, string name, string uniqueName)
    {
        var identifier = parameter.Syntax.Identifier.Span;
        var start = identifier.Start - Prefix.Length;
        var renamed = parameter.Text.Substring(0, start) + uniqueName + parameter.Text.Substring(start + identifier.Length);

        var isBoundByName = !parameter.Syntax.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => NameIndependentAttributes.Contains(attribute.Name.ToString()));

        return isBoundByName
            ? $"[AliasAs(\"{name}\")] {renamed}"
            : renamed;
    }
}
