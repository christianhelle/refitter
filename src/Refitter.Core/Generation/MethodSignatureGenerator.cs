using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class MethodSignatureGenerator(RefitGeneratorSettings settings)
    : IMethodSignatureGenerator
{
    private readonly ParameterListBuilder parameterListBuilder = new(settings);

    public MethodSignature Generate(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType,
        string? formObjectParameterType = null)
    {
        var parameterList = parameterListBuilder.Build(
            operationModel,
            operation,
            dynamicQuerystringParameterType,
            formObjectParameterType);

        var parametersString = string.Join(", ", parameterList.Parameters);
        return new MethodSignature(
            parameterList.Path,
            parametersString,
            parameterList.Parameters,
            parameterList.DynamicQuerystringCode)
        {
            FormObjectParameters = parameterList.FormObjectCode,
        };
    }
}
