using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// This is a thin facade that delegates to <see cref="RefitPipeline"/> and <see cref="RefitCodeGenerator"/>.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    private static readonly IRefitCodeGenerator CodeGenerator = new RefitCodeGenerator();

    /// <summary>
    /// OpenAPI specifications used to generate Refit clients and interfaces.
    /// This is the filtered/cleaned document after pipeline processing.
    /// </summary>
    public OpenApiDocument OpenApiDocument => document;

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class synchronously
    /// from a pre-loaded <see cref="OpenApiDocument"/>.
    /// </summary>
    public static RefitGenerator Create(OpenApiDocument document, RefitGeneratorSettings settings)
        => RefitPipeline.Create(document, settings);

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// </summary>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
        => await RefitPipeline.CreateAsync(settings).ConfigureAwait(false);

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate() => CodeGenerator.Generate(document, settings);

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles() => CodeGenerator.GenerateMultipleFiles(document, settings);
}
