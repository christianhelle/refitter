namespace Refitter.Core;

/// <summary>
/// Enum representing the different options for generating multiple Refit interfaces.
/// </summary>
public enum MultipleInterfaces
{
    /// <summary>
    /// Do not generate multiple interfaces
    /// </summary>
    Unset,

    /// <summary>
    /// Generate a Refit interface for each endpoint with a single Execute() method. 
    /// The method name can be customized using the <see cref="RefitGeneratorSettings.OperationNameTemplate"/> setting.
    /// </summary>
    ByEndpoint,

    /// <summary>
    /// Generate a Refit interface for each tag
    /// </summary>
    ByTag
}