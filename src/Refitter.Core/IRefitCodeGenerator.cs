using NSwag;

namespace Refitter.Core;

public interface IRefitCodeGenerator
{
    string Generate(OpenApiDocument document, RefitGeneratorSettings settings);

    GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings);
}
