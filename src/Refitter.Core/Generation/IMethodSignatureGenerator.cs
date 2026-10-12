using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal interface IMethodSignatureGenerator
{
    MethodSignature Generate(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        string dynamicQuerystringParameterType);
}
