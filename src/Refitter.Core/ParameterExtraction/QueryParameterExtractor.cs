using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal sealed class QueryParameterExtractor
{
    public (IReadOnlyList<string> Parameters, string? DynamicQuerystringCode) Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType)
    {
        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .ToList();

        return settings.UseDynamicQuerystringParameters && queryParameters.Count >= 2
            ? ExtractDynamic(queryParameters, settings, dynamicQuerystringParameterType)
            : (ExtractSimple(queryParameters, settings), null);
    }

    private static (IReadOnlyList<string> Parameters, string? DynamicQuerystringCode) ExtractDynamic(
        List<CSharpParameterModel> queryParameters,
        RefitGeneratorSettings settings,
        string dynamicQuerystringParameterType)
    {
        var dynamicQuerystringCode = DynamicQuerystringParameterBuilder.Build(
            queryParameters,
            dynamicQuerystringParameterType,
            settings);

        var code = !string.IsNullOrWhiteSpace(dynamicQuerystringCode)
            ? dynamicQuerystringCode
            : null;

        var allNullable = queryParameters.All(p =>
            ParameterShared.GetQueryParameterType(p, settings).EndsWith("?"));

        var dynamicQuerystringParameter = $"[Query] {dynamicQuerystringParameterType}";
        if (allNullable)
            dynamicQuerystringParameter += "?";
        dynamicQuerystringParameter += " queryParams";

        return (new[] { dynamicQuerystringParameter }, code);
    }

    private static List<string> ExtractSimple(
        List<CSharpParameterModel> queryParameters,
        RefitGeneratorSettings settings)
    {
        return queryParameters
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                return $"{ParameterShared.JoinAttributes(ParameterShared.GetQueryAttribute(p, settings), ParameterShared.GetAliasAsAttribute(p.Name, variableName))}{ParameterShared.GetQueryParameterType(p, settings)} {variableName}";
            })
            .ToList();
    }
}
