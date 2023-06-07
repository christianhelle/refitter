using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;
using System;

namespace Refitter.Core;

public class OperationNameGenerator : IOperationNameGenerator
{
    private readonly IOperationNameGenerator defaultOperationNameGenerator =
        new MultipleClientsFromOperationIdOperationNameGenerator();

    public bool SupportsMultipleClients => throw new NotImplementedException();

    public string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        return defaultOperationNameGenerator.GetClientName(document, path, httpMethod, operation);
    }

    public string GetOperationName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        return defaultOperationNameGenerator
            .GetOperationName(document, path, httpMethod, operation)
            .CapitalizeFirstCharacter()
            .ConvertKebabCaseToPascalCase()
            .ConvertRouteToCamelCase();
    }
}
