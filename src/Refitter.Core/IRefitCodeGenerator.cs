using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit client code from a cleaned OpenAPI document.
/// Produces single-file or multi-file output.
/// </summary>
public interface IRefitCodeGenerator
{
    /// <summary>
    /// Generates all Refit code as a single string.
    /// </summary>
    /// <param name="document">The OpenAPI document to generate code from.</param>
    /// <param name="settings">The generator settings.</param>
    /// <returns>The generated C# code.</returns>
    string Generate(OpenApiDocument document, RefitGeneratorSettings settings);

    /// <summary>
    /// Generates Refit code as multiple files (interfaces, contracts, DI, serializer context).
    /// </summary>
    /// <param name="document">The OpenAPI document to generate code from.</param>
    /// <param name="settings">The generator settings.</param>
    /// <returns>A collection of generated code files.</returns>
    GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings);
}
