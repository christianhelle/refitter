using System.Diagnostics.CodeAnalysis;
using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;
using GeneratorType = Refitter.Core.OperationNameGeneratorTypes;

namespace Refitter.Core;

internal class OperationNameGenerator : IOperationNameGenerator
{
    private readonly IOperationNameGenerator defaultGenerator;

    public OperationNameGenerator(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        switch (settings.OperationNameGenerator)
        {
            case GeneratorType.MultipleClientsFromOperationId:
                defaultGenerator = new MultipleClientsFromOperationIdOperationNameGenerator();
                break;

            case GeneratorType.MultipleClientsFromPathSegments:
                defaultGenerator = new MultipleClientsFromPathSegmentsOperationNameGenerator();
                break;

            case GeneratorType.MultipleClientsFromFirstTagAndOperationId:
                defaultGenerator = new MultipleClientsFromFirstTagAndOperationIdGenerator();
                break;

            case GeneratorType.MultipleClientsFromFirstTagAndOperationName:
                defaultGenerator = new MultipleClientsFromFirstTagAndOperationNameGenerator();
                break;

            case GeneratorType.MultipleClientsFromFirstTagAndPathSegments:
                defaultGenerator = new MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator();
                break;

            case GeneratorType.SingleClientFromOperationId:
                defaultGenerator = new SingleClientFromOperationIdOperationNameGenerator();
                break;

            case GeneratorType.SingleClientFromPathSegments:
                defaultGenerator = new SingleClientFromPathSegmentsOperationNameGenerator();
                break;

            default:
                defaultGenerator = new MultipleClientsFromOperationIdOperationNameGenerator();
                if (CheckForDuplicateOperationIds(document))
                    defaultGenerator = new MultipleClientsFromFirstTagAndPathSegmentsOperationNameGenerator();
                break;
        }
    }

    [ExcludeFromCodeCoverage]
    public bool SupportsMultipleClients => defaultGenerator.SupportsMultipleClients;

    [ExcludeFromCodeCoverage]
    public string GetClientName(OpenApiDocument document, string path, string httpMethod, OpenApiOperation operation)
    {
        return defaultGenerator.GetClientName(document, path, httpMethod, operation);
    }

    public string GetOperationName(
        OpenApiDocument document,
        string path,
        string httpMethod,
        OpenApiOperation operation) =>
        defaultGenerator
            .GetOperationName(document, path, httpMethod, operation)
            .ConvertKebabCaseToPascalCase()
            .ConvertSnakeCaseToPascalCase()
            .ConvertRouteToCamelCase()
            .ConvertSpacesToPascalCase()
            .ConvertColonsToPascalCase()
            .Sanitize()
            .CapitalizeFirstCharacter();

    private bool CheckForDuplicateOperationIds(
        OpenApiDocument document)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in document.Paths)
        {
            foreach (var operations in kv.Value)
            {
                var operation = operations.Value;
                var operationName = GetOperationName(
                    document,
                    kv.Key,
                    operations.Key,
                    operation);

                if (!seen.Add(operationName))
                    return true; // Short-circuit on first duplicate
            }
        }

        return false;
    }
}
