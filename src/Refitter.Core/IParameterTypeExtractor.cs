using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal interface IParameterTypeExtractor
{
    bool CanExtract(OpenApiParameterKind kind);
    IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings);
}
