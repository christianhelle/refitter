using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal interface IMethodSignatureGenerator
{
    (string ParametersString, IReadOnlyList<string> Parameters, string? DynamicQuerystringParameters) Generate(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType);
}
