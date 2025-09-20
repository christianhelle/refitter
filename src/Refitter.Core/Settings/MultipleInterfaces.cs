using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// Enum representing the different options for generating multiple Refit interfaces.
/// </summary>
[Description("Enum representing the different options for generating multiple Refit interfaces.")]
public enum MultipleInterfaces
{
    /// <summary>
    /// Do not generate multiple interfaces
    /// </summary>
    [Description("Do not generate multiple interfaces")]
    Unset,

    /// <summary>
    /// Generate a Refit interface for each endpoint with a single Execute() method.
    /// The method name can be customized using <see cref="RefitGeneratorSettings.OperationNameTemplate"/>, where {operationName} is replaced with 'Execute'.
    /// </summary>
    [Description(
        """
            Generate a Refit interface for each endpoint with a single Execute() method.
            The method name can be customized using OperationNameTemplate, where {operationName} is replaced with 'Execute'.
            """
    )]
    ByEndpoint,

    /// <summary>
    /// Generate a Refit interface for each tag
    /// </summary>
    [Description("Generate a Refit interface for each tag")]
    ByTag
}
