using NSwag;

namespace Refitter.Core;

internal interface IReturnTypeGenerator
{
    string Generate(OpenApiOperation operation);

    bool IsApiResponseType(string typeName);

    bool IsFileStreamResponse(OpenApiOperation operation);
}
