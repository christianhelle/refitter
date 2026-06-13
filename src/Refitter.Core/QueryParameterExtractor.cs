using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class QueryParameterExtractor : IParameterTypeExtractor
{
    public string? DynamicQuerystringParameterType { get; set; }
    public string? DynamicQuerystringCode { get; private set; }

    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var queryParameters = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Query)
            .ToList();

        if (settings.UseDynamicQuerystringParameters && queryParameters.Count >= 2)
            return ExtractDynamic(queryParameters, settings);

        return ExtractSimple(queryParameters, settings);
    }

    private List<string> ExtractDynamic(
        List<CSharpParameterModel> queryParameters,
        RefitGeneratorSettings settings)
    {
        var dynamicQuerystringCode = DynamicQuerystringParameterBuilder.Build(
            queryParameters,
            DynamicQuerystringParameterType!,
            settings);

        DynamicQuerystringCode = !string.IsNullOrWhiteSpace(dynamicQuerystringCode)
            ? dynamicQuerystringCode
            : null;

        var allNullable = queryParameters.All(p =>
            ParameterShared.GetQueryParameterType(p, settings).EndsWith("?"));

        var dynamicQuerystringParameter = $"[Query] {DynamicQuerystringParameterType}";
        if (allNullable)
            dynamicQuerystringParameter += "?";
        dynamicQuerystringParameter += " queryParams";
        return [dynamicQuerystringParameter];
    }

    private List<string> ExtractSimple(
        List<CSharpParameterModel> queryParameters,
        RefitGeneratorSettings settings)
    {
        DynamicQuerystringCode = string.Empty;

        return queryParameters
            .Select(p =>
            {
                var variableName = ParameterShared.GetVariableName(p);
                return $"{ParameterShared.JoinAttributes(ParameterShared.GetQueryAttribute(p, settings), ParameterShared.GetAliasAsAttribute(p.Name, variableName))}{ParameterShared.GetQueryParameterType(p, settings)} {variableName}";
            })
            .ToList();
    }
}
