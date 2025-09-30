using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class CustomCSharpClientGenerator(OpenApiDocument document, CSharpClientGeneratorSettings settings)
    : CSharpClientGenerator(document, settings)
{
    internal CSharpOperationModel CreateOperationModel(OpenApiOperation operation) =>
        base.CreateOperationModel(operation, Settings);
}
