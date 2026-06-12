using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using Refitter.Core;
using System.Text;

namespace Refitter.Core;

internal static class OptionalParameterReorderer
{
    public static IEnumerable<string> Reorder(
        List<string> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels)
    {
        if (!settings.OptionalParameters || settings.ApizrSettings?.WithRequestOptions == true)
            return parameters;

        var nullablePattern = new System.Text.RegularExpressions.Regex(@"\?\s+@?\w+(\s*=\s*[^,]+)?$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        parameters = parameters.OrderBy(c => nullablePattern.IsMatch(c)).ToList();
        for (int index = 0; index < parameters.Count; index++)
        {
            if (nullablePattern.IsMatch(parameters[index]))
            {
                var parameterString = parameters[index];
                var defaultValue = GetDefaultValueForParameter(parameterString, parameterModels);
                parameters[index] = parameterString + " = " + defaultValue;
            }
        }

        return parameters;
    }

    private static string GetDefaultValueForParameter(string parameterString, ICollection<CSharpParameterModel> parameterModels)
    {
        var parts = parameterString.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "default";

        var variableName = parts[parts.Length - 1].TrimEnd(';', ',');

        var parameterModel = parameterModels.FirstOrDefault(p => p.VariableName == variableName);
        parameterModel ??= parameterModels.FirstOrDefault(p => GetVariableName(p) == variableName);
        if (parameterModel?.Schema?.Default != null && !string.IsNullOrEmpty(parameterModel.Type))
        {
            return FormatDefaultValue(parameterModel.Schema.Default, parameterModel.Type);
        }
        return "default";
    }

    private static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
    }

    private static string FormatDefaultValue(object? defaultValue, string parameterType)
    {
        if (defaultValue == null)
            return "default";

        var type = parameterType.TrimEnd('?').Trim();

        return type switch
        {
            "bool" => defaultValue.ToString()?.ToLowerInvariant() ?? "default",
            "string" => $"\"{EscapeString(defaultValue.ToString() ?? string.Empty)}\"",
            _ when IsNumericType(type) => FormatNumericValue(defaultValue, type),
            _ => "default"
        };
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 10);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\f': sb.Append("\\f"); break;
                case '\v': sb.Append("\\v"); break;
                case '\b': sb.Append("\\b"); break;
                case '\0': sb.Append("\\0"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string FormatNumericValue(object defaultValue, string type)
    {
        var numericString = defaultValue is IFormattable formattable
            ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
            : (defaultValue.ToString() ?? "default");

        return type switch
        {
            "float" or "Single" => $"{numericString}f",
            "decimal" or "Decimal" => $"{numericString}m",
            "double" or "Double" => FormatDoubleLiteral(numericString),
            "long" or "Int64" => $"{numericString}L",
            "ulong" or "UInt64" => $"{numericString}UL",
            "uint" or "UInt32" => $"{numericString}U",
            _ => numericString
        };
    }

    private static string FormatDoubleLiteral(string numericString)
    {
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        return numericString + ".0";
    }

    private static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
    }
}
