using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

public class CustomCSharpClientGenerator : CSharpClientGenerator
{
    public CustomCSharpClientGenerator(OpenApiDocument document, CSharpClientGeneratorSettings settings)
        : base(document, settings)
    {
    }

    public CSharpOperationModel CreateOperationModel(OpenApiOperation operation) =>
        CreateOperationModel(operation, Settings);
}
