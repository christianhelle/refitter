
namespace Refitter.Core;

internal interface IMethodSignatureGenerator
{
    (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) Generate(
        OperationModel operationModel,
        ApiOperation operation,
        string dynamicQuerystringParameterType);
}
