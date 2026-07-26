using System.Text;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

/// <summary>
/// Identifier and string-escaping helpers for generated parameter names.
/// </summary>
internal static class ParameterNaming
{
    public static string ReplaceUnsafeCharacters(string unsafeText)
    {
        return IdentifierUtils.ToCompilableIdentifier(unsafeText);
    }

    public static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 10);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\v':
                    sb.Append("\\v");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\0':
                    sb.Append("\\0");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    public static string ConvertToVariableName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return "value";

        var identifier = IdentifierUtils.ToCompilableIdentifier(propertyName);

        if (identifier.Length > 0 && char.IsUpper(identifier[0]))
        {
            return char.ToLowerInvariant(identifier[0]) + identifier.Substring(1);
        }

        return identifier;
    }

    public static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
    }
}
