using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal interface IMethodAttributeGenerator
{
    string[] Generate(OpenApiOperation operation, CSharpOperationModel operationModel);
}
