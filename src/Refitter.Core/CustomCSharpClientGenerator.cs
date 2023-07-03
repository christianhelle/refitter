using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class CustomCSharpClientGenerator : CSharpClientGenerator
{
    internal CustomCSharpClientGenerator(OpenApiDocument document, CSharpClientGeneratorSettings settings)
        : base(document, settings)
    {
    }

    internal CSharpOperationModel CreateOperationModel(OpenApiOperation operation) =>
        CreateOperationModel(operation, Settings);
}
