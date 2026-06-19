namespace Refitter.Core;

/// <summary>
/// Configuration for code generation behavior.
/// </summary>
public interface ICodeGenerationConfiguration
{
    /// <summary>
    /// Gets the NSwag code generator settings.
    /// </summary>
    CodeGeneratorSettings? CodeGeneratorSettings { get; }

    /// <summary>
    /// Gets a value indicating whether to return <c>IApiResponse</c> objects.
    /// </summary>
    bool ReturnIApiResponse { get; }

    /// <summary>
    /// Gets a value indicating whether to return IObservable or Task.
    /// </summary>
    bool ReturnIObservable { get; }

    /// <summary>
    /// Gets the dictionary of operation ids and a specific response type that they should use.
    /// </summary>
    Dictionary<string, string> ResponseTypeOverride { get; }

    /// <summary>
    /// Gets a value indicating whether contracts should be generated.
    /// </summary>
    bool GenerateContracts { get; }

    /// <summary>
    /// Gets a value indicating whether clients should be generated.
    /// </summary>
    bool GenerateClients { get; }

    /// <summary>
    /// Gets a value indicating whether to generate JsonSerializerContext for AOT compilation.
    /// </summary>
    bool GenerateJsonSerializerContext { get; }
}
