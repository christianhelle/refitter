using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class MethodSignatureGenerator(
    RefitGeneratorSettings settings,
    IParameterExtractor? parameterExtractor = null)
    : IMethodSignatureGenerator
{
    private readonly IParameterExtractor parameterExtractor = parameterExtractor ?? new ParameterAggregator();

    public (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) Generate(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType)
    {
        var parameters = parameterExtractor.ExtractParameters(
                operationModel,
                operation,
                settings,
                dynamicQuerystringParameterType,
                out var operationDynamicQuerystringParameters)
            .ToList();

        var parametersString = string.Join(", ", parameters);
        return (parametersString, parameters, operationDynamicQuerystringParameters);
    }
}
