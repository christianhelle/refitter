using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// The Authentication header style to use.
/// </summary>
[Description("The Authentication header style to use")]
public enum AuthenticationHeaderStyle {
    /// <summary>
    /// Do not generate any "Authorization" header attributes.
    /// </summary>
    None,

    /// <summary>
    /// Generate a "Authorization" header as a method attribute.
    /// </summary>
    Method,

    /// <summary>
    /// Generate a "Authorization" header as a method parameter attribute.
    /// </summary>
    Parameter,
}