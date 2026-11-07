namespace Refitter.Core;

/// <summary>
/// A parameter of a generated Refit method. The parameter rules (deduplication, route disambiguation,
/// optional reordering, documentation) work on these records, and the parameter is rendered as C# once.
/// </summary>
#pragma warning disable S1186 // Positional record constructor is compiler-generated
internal sealed record MethodParameter(string Type, string Name)
#pragma warning restore S1186
{
    private static readonly HashSet<string> NameIndependentAttributes = new(StringComparer.Ordinal)
    {
        "Header",
        "Body",
    };

    /// <summary>
    /// The Refit attributes other than <c>AliasAs</c>, like <c>Query</c> or <c>Header("X-Key")</c>.
    /// </summary>
    public IReadOnlyList<string> Attributes { get; init; } = [];

    /// <summary>
    /// The name Refit sends instead of <see cref="Name"/>, rendered as an <c>AliasAs</c> attribute.
    /// </summary>
    public string? Alias { get; init; }

    /// <summary>
    /// Whether <see cref="Alias"/> is rendered in its own attribute list, ahead of <see cref="Attributes"/>,
    /// as it is when a parameter is renamed after extraction.
    /// </summary>
    public bool IsAliasSeparate { get; init; }

    /// <summary>
    /// The default value, rendered after <c>=</c>.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// The parameter name without the <c>@</c> that escapes a C# keyword.
    /// </summary>
    public string Identifier => Name.TrimStart('@');

    /// <summary>
    /// The name Refit binds the parameter by, like a route placeholder or query key.
    /// </summary>
    public string BoundName => Alias ?? Identifier;

    /// <summary>
    /// Whether the parameter is bound by its name, as opposed to an alias, a header name or the body.
    /// </summary>
    public bool IsBoundByName =>
        Alias is null &&
        !Attributes.Any(attribute => NameIndependentAttributes.Contains(GetAttributeName(attribute)));

    /// <summary>
    /// Whether the parameter type is nullable, which makes it optional when optional parameters are enabled.
    /// </summary>
    public bool IsNullable => Type.EndsWith("?", StringComparison.Ordinal);

    public static string Render(IEnumerable<MethodParameter> parameters) =>
        string.Join(", ", parameters.Select(parameter => parameter.Render()));

    public string Render()
    {
        var aliasAttribute = Alias is null ? null : $"AliasAs(\"{ParameterNaming.EscapeString(Alias)}\")";
        var separateAlias = aliasAttribute is not null && IsAliasSeparate ? $"[{aliasAttribute}] " : string.Empty;
        var attributes = aliasAttribute is not null && !IsAliasSeparate
            ? Attributes.Concat([aliasAttribute]).ToList()
            : Attributes;
        var attributeList = attributes.Count == 0 ? string.Empty : $"[{string.Join(", ", attributes)}] ";
        var defaultValue = DefaultValue is null ? string.Empty : $" = {DefaultValue}";

        return $"{separateAlias}{attributeList}{Type} {Name}{defaultValue}";
    }

    public override string ToString() => Render();

    private static string GetAttributeName(string attribute)
    {
        var argumentsStart = attribute.IndexOf('(');
        return (argumentsStart < 0 ? attribute : attribute.Substring(0, argumentsStart)).Trim();
    }
}
