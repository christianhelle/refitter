using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// Represents a type override mapping for custom formats in schema definitions
/// </summary>
public class TypeOverride
{
    /// <summary>
    /// Gets or sets the format pattern to match (e.g., "string:my-date-time")
    /// </summary>
    [Description("The format pattern to match (e.g., \"string:my-date-time\")")]
    public string FormatPattern { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the .NET type to use for this format (e.g., "Domain.Specific.DataType")
    /// </summary>
    [Description("The .NET type to use for this format (e.g., \"Domain.Specific.DataType\")")]
    public string TypeName { get; set; } = string.Empty;
}
