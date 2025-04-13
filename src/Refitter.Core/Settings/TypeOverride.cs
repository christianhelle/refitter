namespace Refitter.Core;

/// <summary>
/// Defines a type override to be used with NJsonSchema.
/// This allows you to override the type name and format
/// of a specific type.
/// </summary>
public record TypeOverride(string Type, string Format, string TypeName);
