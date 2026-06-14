using NSwag;

namespace Refitter.Core;

public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    private static readonly IRefitCodeGenerator CodeGenerator = new RefitCodeGenerator();

    public OpenApiDocument OpenApiDocument => document;

    public static RefitGenerator Create(OpenApiDocument document, RefitGeneratorSettings settings)
        => RefitPipeline.Create(document, settings);

    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
        => await RefitPipeline.CreateAsync(settings).ConfigureAwait(false);

    public string Generate() => CodeGenerator.Generate(document, settings);

    public GeneratorOutput GenerateMultipleFiles() => CodeGenerator.GenerateMultipleFiles(document, settings);
}
