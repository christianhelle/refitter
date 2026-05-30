namespace Refitter;

/// <summary>
/// Parses the textual OpenAPI statistics produced during validation into
/// label/value pairs and maps each metric to its display icon and description.
/// Shared by the simple and rich reporters so the parsing rules live in one place.
/// </summary>
internal static class OpenApiStatisticsFormatter
{
    public static IReadOnlyList<(string Label, string Value)> Parse(string statistics)
    {
        var result = new List<(string Label, string Value)>();
        var lines = statistics.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Trim().StartsWith("-"))
                continue;

            var parts = line.Trim().TrimStart('-').Trim().Split(':', 2);
            if (parts.Length == 2)
                result.Add((parts[0].Trim(), parts[1].Trim()));
        }

        return result;
    }

    public static string GetIcon(string label) =>
        label.ToLowerInvariant() switch
        {
            var l when l.Contains("path") => "📝",
            var l when l.Contains("operation") => "⚡",
            var l when l.Contains("parameter") => "📝",
            var l when l.Contains("request") => "📤",
            var l when l.Contains("response") => "📥",
            var l when l.Contains("link") => "🔗",
            var l when l.Contains("callback") => "🔄",
            var l when l.Contains("schema") => "📋",
            _ => "📊"
        };

    public static string GetDescription(string label) =>
        label.ToLowerInvariant() switch
        {
            var l when l.Contains("path") => "API endpoints defined",
            var l when l.Contains("operation") => "HTTP operations available",
            var l when l.Contains("parameter") => "Input parameters defined",
            var l when l.Contains("request") => "Request body schemas",
            var l when l.Contains("response") => "Response schemas defined",
            var l when l.Contains("link") => "Operation links",
            var l when l.Contains("callback") => "Callback definitions",
            var l when l.Contains("schema") => "Data schemas defined",
            _ => "API specification metric"
        };
}
