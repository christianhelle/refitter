namespace Refitter.Core;

/// <summary>
/// Provides the C# property names of generated contract types.
/// </summary>
/// <remarks>
/// Names returned by the provider are made unique per contract type: a name that collides with another
/// property, the type name or the generated <c>AdditionalProperties</c> dictionary gets a numeric suffix.
/// The wire name is kept by the generated <c>JsonPropertyName</c> attribute.
/// </remarks>
public interface IPropertyNameProvider
{
    /// <summary>
    /// Gets the C# property name for a schema property.
    /// </summary>
    /// <param name="context">The schema property to name.</param>
    /// <returns>A valid C# identifier.</returns>
    string GetPropertyName(PropertyNameContext context);
}

/// <summary>
/// Describes a schema property that needs a C# property name.
/// </summary>
/// <param name="Name">The property name as it appears in the OpenAPI document.</param>
/// <param name="IsRequired">Whether the property is listed as required by its schema.</param>
public sealed record PropertyNameContext(string Name, bool IsRequired);
