
namespace Refitter.Core;

internal class MethodSignatureGenerator(RefitGeneratorSettings settings)
    : IMethodSignatureGenerator
{
    private readonly ParameterListBuilder parameterListBuilder = new(settings);

    public (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) Generate(
        OperationModel operationModel,
        ApiOperation operation,
        string dynamicQuerystringParameterType)
    {
        var parameterList = parameterListBuilder.Build(
            operationModel,
            operation,
            dynamicQuerystringParameterType);

        var parametersString = string.Join(", ", parameterList.Parameters);
        return (parametersString, parameterList.Parameters, parameterList.DynamicQuerystringCode);
    }
}
