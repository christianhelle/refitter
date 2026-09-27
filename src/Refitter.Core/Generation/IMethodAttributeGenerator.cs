
namespace Refitter.Core;

internal interface IMethodAttributeGenerator
{
    string[] Generate(ApiOperation operation, OperationModel operationModel);
}
