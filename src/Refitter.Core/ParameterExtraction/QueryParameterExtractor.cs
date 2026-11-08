using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class QueryParameterExtractor
{
    public static (IReadOnlyList<MethodParameter> Parameters, string? DynamicQuerystringCode) Extract(
        CSharpOperationModel operationModel,
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

    private static (IReadOnlyList<MethodParameter> Parameters, string? DynamicQuerystringCode) ExtractDynamic(
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
            ParameterTypeResolver.GetQueryParameterType(p, settings).EndsWith('?'));

        var dynamicQuerystringParameter = new MethodParameter(
            allNullable ? $"{dynamicQuerystringParameterType}?" : dynamicQuerystringParameterType,
            "queryParams")
        {
            Attributes = ["Query"],
        };

        return (new[] { dynamicQuerystringParameter }, code);
    }

    private static List<MethodParameter> ExtractSimple(
        List<CSharpParameterModel> queryParameters,
        RefitGeneratorSettings settings)
    {
        return queryParameters
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return new MethodParameter(ParameterTypeResolver.GetQueryParameterType(p, settings), variableName)
                {
                    Attributes = [ParameterAttributeFormatter.GetQueryAttribute(p, settings)],
                    Alias = ParameterAttributeFormatter.GetAlias(p.Name, variableName),
                };
            })
            .ToList();
    }
}
