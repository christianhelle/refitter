using System.Text;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Generates JsonSerializerContext for AOT compilation support
/// </summary>
internal static class JsonSerializerContextGenerator
{
    /// <summary>
    /// Generates a JsonSerializerContext class with all DTO types registered for source generation
    /// </summary>
    /// <param name="contracts">The generated contracts code containing the DTO types</param>
    /// <param name="settings">The generator settings</param>
    /// <returns>The generated JsonSerializerContext code</returns>
    public static string Generate(string contracts, RefitGeneratorSettings settings)
    {
        var typeNames = ExtractTypeNames(contracts);
        if (typeNames.Count == 0)
            return string.Empty;

        var contextName = $"{settings.Naming.InterfaceName}SerializerContext";
        var sb = new StringBuilder();

        // Add all JsonSerializable attributes
        foreach (var typeName in typeNames.OrderBy(t => t))
        {
            sb.AppendLine($"[JsonSerializable(typeof({typeName}))]");
        }

        // Add context class
        sb.AppendLine($"internal partial class {contextName} : JsonSerializerContext");
        sb.AppendLine("{");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Extracts type names (classes, records, enums) from generated contracts
    /// </summary>
    private static HashSet<string> ExtractTypeNames(string contracts)
    {
        var typeNames = new HashSet<string>();

        // Match class declarations: public class TypeName, internal class TypeName
        var classPattern = new Regex(
            @"^\s*(?:public|internal)\s+(?:partial\s+)?(?:class|record)\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        foreach (Match match in classPattern.Matches(contracts))
        {
            if (match.Success && match.Groups.Count > 1)
            {
                typeNames.Add(match.Groups[1].Value);
            }
        }

        // Match enum declarations: public enum TypeName, internal enum TypeName
        var enumPattern = new Regex(
            @"^\s*(?:public|internal)\s+enum\s+([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(1));

        foreach (Match match in enumPattern.Matches(contracts))
        {
            if (match.Success && match.Groups.Count > 1)
            {
                typeNames.Add(match.Groups[1].Value);
            }
        }

        return typeNames;
    }
}
