using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class MethodSignatureGenerator(RefitGeneratorSettings settings)
    : IMethodSignatureGenerator
{
    private readonly ParameterListBuilder parameterListBuilder = new(settings);

    public (string Path, string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) Generate(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType)
    {
        var parameterList = parameterListBuilder.Build(
            operationModel,
            operation,
            dynamicQuerystringParameterType);

        var parametersString = string.Join(", ", parameterList.Parameters);
        return (parameterList.Path, parametersString, parameterList.Parameters, parameterList.DynamicQuerystringCode);
    }
}
