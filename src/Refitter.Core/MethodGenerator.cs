using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class MethodGenerator(
    IReturnTypeGenerator returnTypeGenerator,
    IMethodAttributeGenerator methodAttributeGenerator,
    IMethodSignatureGenerator methodSignatureGenerator)
    : IMethodGenerator
{

    public string GenerateReturnType(OpenApiOperation operation) =>
        returnTypeGenerator.Generate(operation);

    public bool IsApiResponseType(string typeName) =>
        returnTypeGenerator.IsApiResponseType(typeName);

    public string[] GenerateMethodAttributes(OpenApiOperation operation, CSharpOperationModel operationModel) =>
        methodAttributeGenerator.Generate(operation, operationModel);

    public (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) GenerateMethodSignature(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType) =>
        methodSignatureGenerator.Generate(operationModel, operation, dynamicQuerystringParameterType);
}
