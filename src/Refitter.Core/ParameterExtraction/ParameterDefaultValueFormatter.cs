using System.Globalization;
using System.Text.RegularExpressions;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

/// <summary>
/// Formats default values and numeric literals for generated parameters.
/// </summary>
internal static class ParameterDefaultValueFormatter
{
    private const string DefaultKeyword = "default";

    // The value is written into the generated code as it is, so anything else is replaced by default
    private static readonly Regex NumberLiteral = new(
        @"^-?[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static bool IsNumericType(string type)
    {
        return type is "int" or "Int32" or "long" or "Int64" or "short" or "Int16"
            or "byte" or "Byte" or "decimal" or "Decimal" or "float" or "Single"
            or "double" or "Double" or "sbyte" or "SByte" or "uint" or "UInt32"
            or "ulong" or "UInt64" or "ushort" or "UInt16";
    }

    private static string FormatDoubleLiteral(string numericString)
    {
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        return numericString + ".0";
    }

    public static string FormatNumericValue(object defaultValue, string type)
    {
        var numericString = defaultValue is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : (defaultValue.ToString() ?? DefaultKeyword);

        if (defaultValue is bool || !NumberLiteral.IsMatch(numericString))
            return DefaultKeyword;

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

    public static string FormatDefaultValue(object? defaultValue, string parameterType)
    {
        if (defaultValue == null)
            return DefaultKeyword;

        var type = parameterType.TrimEnd('?').Trim();

        return type switch
        {
            "bool" => FormatBooleanValue(defaultValue),
            "string" => $"\"{ParameterNaming.EscapeString(defaultValue.ToString() ?? string.Empty)}\"",
            _ when IsNumericType(type) => FormatNumericValue(defaultValue, type),
            _ => DefaultKeyword
        };
    }

    private static string FormatBooleanValue(object defaultValue)
    {
        var text = defaultValue.ToString()?.ToLowerInvariant();
        return text is "true" or "false" ? text : DefaultKeyword;
    }

    public static string GetDefaultValueForParameter(
        string variableName,
        ICollection<CSharpParameterModel> parameterModels)
    {
        var parameterModel = parameterModels.FirstOrDefault(p => p.VariableName == variableName);
        parameterModel ??= parameterModels.FirstOrDefault(p => ParameterNaming.GetVariableName(p) == variableName);
        if (parameterModel?.Schema?.Default != null && !string.IsNullOrEmpty(parameterModel.Type))
        {
            return FormatDefaultValue(parameterModel.Schema.Default, parameterModel.Type);
        }

        return DefaultKeyword;
    }
}
