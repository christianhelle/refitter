using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// Configuration for filtering which endpoints and schemas to include in generated code.
/// </summary>
[ExcludeFromCodeCoverage]
public class FilterConfig
{
    /// <summary>
    /// Set to <c>true</c> to only include Endpoints that contain this tag.
    /// May be set multiple times and result in OR'ed evaluation.
    /// </summary>
    [Description("Generate a Refit interface for each endpoint.")]
    public string[] IncludeTags { get; set; } = [];

    /// <summary>
    /// Set to <c>true</c> to only include Paths that match the provided regular expression.
    /// May be set multiple times
    /// </summary>
    [Description("Only include Paths that match the provided regular expression. May be set multiple times.")]
    public string[] IncludePathMatches { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of headers to omit from operation signatures. (default: [])
    /// </summary>
    [Description("A collection of headers to omit from operation signatures.")]
    public string[] IgnoredOperationHeaders { get; set; } = [];

    /// <summary>
    /// Exclude namespaces on generated types
    /// </summary>
    [Description("Exclude namespaces on generated types.")]
    public string[] ExcludeNamespaces { get; set; } = [];

    /// <summary>
    /// Add additional namespace to generated types
    /// </summary>
    [Description("Add additional namespace to generated types.")]
    public string[] AdditionalNamespaces { get; set; } = [];
}
