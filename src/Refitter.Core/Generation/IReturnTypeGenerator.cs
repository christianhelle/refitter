
namespace Refitter.Core;

internal interface IReturnTypeGenerator
{
    string Generate(ApiOperation operation);

    bool IsApiResponseType(string typeName);
}
