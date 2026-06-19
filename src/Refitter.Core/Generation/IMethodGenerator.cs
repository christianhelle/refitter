using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal interface IMethodGenerator
{
    string GenerateReturnType(OpenApiOperation operation);

    bool IsApiResponseType(string typeName);

    string[] GenerateMethodAttributes(OpenApiOperation operation, CSharpOperationModel operationModel);

    (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) GenerateMethodSignature(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType);
}
