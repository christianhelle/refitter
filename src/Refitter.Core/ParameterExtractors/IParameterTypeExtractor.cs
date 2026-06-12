using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using System.Text;

namespace Refitter.Core;

internal interface IParameterTypeExtractor
{
    bool CanExtract(OpenApiParameterKind kind);

    bool CanExtract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings);

    IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string? dynamicQuerystringParameterType = null);
}
