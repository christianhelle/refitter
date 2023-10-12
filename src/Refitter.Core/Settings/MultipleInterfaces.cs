using System.Text.Json.Serialization;

namespace Refitter.Core;

/// <summary>
/// Enum representing the different options for generating multiple Refit interfaces.
/// </summary>
public enum MultipleInterfaces
{
    /// <summary>
    /// Do not generate multiple interfaces
    /// </summary>
    [JsonPropertyName("unset")] Unset,

    /// <summary>
    /// Generate a Refit interface for each endpoint with a single Execute() method. 
    /// The method name can be customized using the --operation-name-template command line option, 
    /// or the operationNameTemplate property in the settings file.
    /// </summary>
    [JsonPropertyName("byEndpoint")] ByEndpoint,

    /// <summary>
    /// Generate a Refit interface for each tag
    /// </summary>
    [JsonPropertyName("byTag")] ByTag
}