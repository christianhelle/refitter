using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using NSwag.CodeGeneration;

namespace Refitter.Core;

/// <summary>
/// Configuration for method parameter code generation options.
/// </summary>
[ExcludeFromCodeCoverage]
public class ParameterConfig
{
    /// <summary>
    /// Enable or disable the use of cancellation tokens.
    /// </summary>
    [Description("Enable or disable the use of cancellation tokens.")]
    public bool UseCancellationTokens { get; set; }

    /// <summary>
    /// Set to <c>true</c> to explicitly format date query string parameters
    /// in ISO 8601 standard date format using delimiters (for example: 2023-06-15)
    /// </summary>
    [Description(
        """
        Set to true to explicitly format date query string parameters
        in ISO 8601 standard date format using delimiters (for example: 2023-06-15)
        """
    )]
    public bool UseIsoDateFormat { get; set; }

    /// <summary>
    /// Set to <c>true</c> to re-order optional parameters to the end of the parameter list
    /// </summary>
    [Description("Re-order optional parameters to the end of the parameter list.")]
    public bool OptionalParameters { get; set; }

    /// <summary>
    /// Set to <c>true</c> to wrap multiple query parameters into a single complex one. Default is <c>false</c> (no wrapping).
    /// See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information.
    /// </summary>
    [Description(
        """
        Wrap multiple query parameters into a single complex one.
        See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information.
        """
    )]
    public bool UseDynamicQuerystringParameters { get; set; }

    /// <summary>
    /// Gets or sets the collection format to use for array query parameters.
    /// Default is CollectionFormat.Multi.
    /// </summary>
    [Description("The collection format to use for array query parameters. Default is CollectionFormat.Multi.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.Multi;

    /// <summary>
    /// Gets or sets the parameter name generator for customizing parameter names.
    /// </summary>
    [JsonIgnore]
    public IParameterNameGenerator? ParameterNameGenerator { get; set; }
}
