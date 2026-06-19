namespace Refitter.Core;

/// <summary>
/// Configuration for naming conventions of generated code elements.
/// </summary>
public interface INamingConfiguration
{
    /// <summary>
    /// Gets the naming settings.
    /// </summary>
    NamingSettings Naming { get; }

    /// <summary>
    /// Gets how generated contract properties are named.
    /// </summary>
    PropertyNamingPolicy PropertyNamingPolicy { get; }

    /// <summary>
    /// Gets the suffix to append to all generated contract type names.
    /// </summary>
    string? ContractTypeSuffix { get; }

    /// <summary>
    /// Gets the namespace for the generated code.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets the namespace for the generated contracts.
    /// </summary>
    string? ContractsNamespace { get; }

    /// <summary>
    /// Gets additional namespaces to include in generated types.
    /// </summary>
    string[] AdditionalNamespaces { get; }

    /// <summary>
    /// Gets namespaces to exclude from generated types.
    /// </summary>
    string[] ExcludeNamespaces { get; }
}
