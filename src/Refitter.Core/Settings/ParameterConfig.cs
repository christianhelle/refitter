using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using NSwag.CodeGeneration;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class ParameterConfig
{
    [Description("Enable or disable the use of cancellation tokens.")]
    public bool UseCancellationTokens { get; set; }

    [Description(
        """
        Set to true to explicitly format date query string parameters
        in ISO 8601 standard date format using delimiters (for example: 2023-06-15)
        """
    )]
    public bool UseIsoDateFormat { get; set; }

    [Description("Re-order optional parameters to the end of the parameter list.")]
    public bool OptionalParameters { get; set; }

    [Description(
        """
        Wrap multiple query parameters into a single complex one.
        See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information.
        """
    )]
    public bool UseDynamicQuerystringParameters { get; set; }

    [Description("The collection format to use for array query parameters. Default is CollectionFormat.Multi.")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CollectionFormat CollectionFormat { get; set; } = CollectionFormat.Multi;

    [JsonIgnore]
    public IParameterNameGenerator? ParameterNameGenerator { get; set; }
}
