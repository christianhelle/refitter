using System.Text.Json.Nodes;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// How the fields of one kind of object are read: fixed field names, and patterns such as <c>x-</c> extensions.
/// </summary>
/// <remarks>Reproduces the fixed and pattern field maps of Microsoft.OpenApi (MIT license).</remarks>
internal sealed class FieldMap<T>
{
    public Dictionary<string, Action<T, JsonNode, ParsingContext>> Fixed { get; } = new(StringComparer.Ordinal);

    public List<(Func<string, bool> Matches, Action<T, string, JsonNode, ParsingContext> Read)> Patterns { get; } = [];

    public FieldMap<T> Field(string name, Action<T, JsonNode, ParsingContext> read)
    {
        Fixed[name] = read;
        return this;
    }

    public FieldMap<T> Without(string name)
    {
        Fixed.Remove(name);
        return this;
    }

    public FieldMap<T> Pattern(Func<string, bool> matches, Action<T, string, JsonNode, ParsingContext> read)
    {
        Patterns.Add((matches, read));
        return this;
    }

    /// <summary>
    /// Accepts <c>x-</c> extensions, which are not validated.
    /// </summary>
    public FieldMap<T> Extensions() =>
        Pattern(IsExtension, (_, _, _, _) => { });

    public static bool IsExtension(string name) =>
        name.StartsWith("x-", StringComparison.OrdinalIgnoreCase);
}
