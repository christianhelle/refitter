using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
public class FilterConfig
{
    [Description("Generate a Refit interface for each endpoint.")]
    public string[] IncludeTags { get; set; } = [];

    [Description("Only include Paths that match the provided regular expression. May be set multiple times.")]
    public string[] IncludePathMatches { get; set; } = [];

    [Description("A collection of headers to omit from operation signatures.")]
    public string[] IgnoredOperationHeaders { get; set; } = [];

    [Description("Exclude namespaces on generated types.")]
    public string[] ExcludeNamespaces { get; set; } = [];

    [Description("Add additional namespace to generated types.")]
    public string[] AdditionalNamespaces { get; set; } = [];
}
