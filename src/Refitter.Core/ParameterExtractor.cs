using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

internal static class ParameterExtractor
{
    public static IEnumerable<string> GetParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        var aggregator = new ParameterAggregator(settings);
        return aggregator.ExtractParameters(
            operationModel,
            operation,
            settings,
            dynamicQuerystringParameterType,
            out dynamicQuerystringParameters);
    }

    public static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        string.Equals(parameterModel.Name, parameterModel.VariableName)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(parameterModel.Name)}\")";

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

    private static string FormatNumericValue(object defaultValue, string type)
    {
        var numericString = defaultValue is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
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
