using System.Text;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;

namespace Refitter.Core;

internal static class ParameterExtractor
{
    private static readonly ParameterAggregator Aggregator = new();

    public static IEnumerable<string> GetParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        return Aggregator.ExtractParameters(
            operationModel,
            operation,
            settings,
            dynamicQuerystringParameterType,
            out dynamicQuerystringParameters);
    }

    // Reflection-based backward compatibility for ParameterExtractorPrivateCoverageTests

    private static string ReplaceUnsafeCharacters(string unsafeText) =>
        ParameterShared.ReplaceUnsafeCharacters(unsafeText);

    private static List<string> ReOrderNullableParameters(
        List<string> parameters,
        RefitGeneratorSettings settings,
        ICollection<CSharpParameterModel> parameterModels) =>
        OptionalParameterReorderer.Reorder(parameters, settings, parameterModels);

    private static string GetDefaultValueForParameter(
        string parameterString,
        ICollection<CSharpParameterModel> parameterModels) =>
        ParameterShared.GetDefaultValueForParameter(parameterString, parameterModels);

    private static string FormatDefaultValue(object? defaultValue, string parameterType) =>
        ParameterShared.FormatDefaultValue(defaultValue, parameterType);

    private static string EscapeString(string value) =>
        ParameterShared.EscapeString(value);

    private static string FormatNumericValue(object defaultValue, string type) =>
        ParameterShared.FormatNumericValue(defaultValue, type);

    private static string FormatDoubleLiteral(string numericString)
    {
        if (numericString.Contains('.') || numericString.Contains('e') || numericString.Contains('E'))
            return numericString;

        return numericString + ".0";
    }

    private static bool IsNumericType(string type) =>
        ParameterShared.IsNumericType(type);

    private static string GetBodyAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings) =>
        ParameterShared.GetBodyAttribute(parameter, settings);

    private static string GetQueryAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings) =>
        ParameterShared.GetQueryAttribute(parameter, settings);

    private static string GetAliasAsAttribute(CSharpParameterModel parameterModel) =>
        ParameterShared.GetAliasAsAttribute(parameterModel);

    private static string GetAliasAsAttribute(string originalName, string variableName) =>
        ParameterShared.GetAliasAsAttribute(originalName, variableName);

    private static string JoinAttributes(params string[] attributes) =>
        ParameterShared.JoinAttributes(attributes);

    private static string GetParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings) =>
        ParameterShared.GetParameterType(parameterModel, settings);

    private static string GetQueryParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings) =>
        ParameterShared.GetQueryParameterType(parameterModel, settings);

    private static string FindSupportedType(string typeName) =>
        ParameterShared.FindSupportedType(typeName);

    private static List<string> GetQueryParameters(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType,
        out string? dynamicQuerystringParameters)
    {
        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .ToList();

        List<string>? parameters = null;
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();

        if (settings.UseDynamicQuerystringParameters && queryParameters.Count >= 2)
        {
            var dynamicQuerystringCode = DynamicQuerystringParameterBuilder.Build(
                queryParameters,
                dynamicQuerystringParameterType,
                settings);

            if (!string.IsNullOrWhiteSpace(dynamicQuerystringCode))
            {
                dynamicQuerystringParametersCodeBuilder.Append(dynamicQuerystringCode);
            }

            var allNullable = queryParameters.All(p =>
                ParameterShared.GetQueryParameterType(p, settings).EndsWith("?"));

            var dynamicQuerystringParameter = $"[Query] {dynamicQuerystringParameterType}";
            if (allNullable)
                dynamicQuerystringParameter += "?";
            dynamicQuerystringParameter += " queryParams";
            parameters = [dynamicQuerystringParameter];
        }

        dynamicQuerystringParameters = dynamicQuerystringParametersCodeBuilder.Length > 0
            ? dynamicQuerystringParametersCodeBuilder.ToString()
            : null;

        parameters ??= QueryParameterExtractor.ExtractSimple(operationModel, settings).ToList();

        return parameters;
    }

    private static void AppendXmlDocComment(string description, StringBuilder codeBuilder) =>
        ParameterShared.AppendXmlDocComment(description, codeBuilder);

    private static string GetCSharpType(JsonSchema propertySchema, RefitGeneratorSettings settings) =>
        ParameterShared.GetCSharpType(propertySchema, settings);

    private static string GetIntegerTypeName(JsonSchema schema, RefitGeneratorSettings settings) =>
        ParameterShared.GetIntegerTypeName(schema, settings);

    private static string GetArrayType(JsonSchema arraySchema, RefitGeneratorSettings settings) =>
        ParameterShared.GetArrayType(arraySchema, settings);

    private static string ConvertToVariableName(string propertyName) =>
        ParameterShared.ConvertToVariableName(propertyName);

    private static string GetVariableName(ParameterModelBase parameterModel) =>
        ParameterShared.GetVariableName(parameterModel);
}
