using System.Globalization;
using System.Text.Json.Nodes;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Reads values out of JSON nodes the way Microsoft.OpenApi (MIT license) does.
/// </summary>
internal static class JsonNodeExtensions
{
    /// <summary>
    /// Returns the value as a string: numbers keep their text and booleans become <c>True</c> or <c>False</c>.
    /// </summary>
    /// <exception cref="SpecificationException">Thrown when the node is an object or an array.</exception>
    public static string? GetScalarValue(this JsonNode? node)
    {
        if (node is not JsonValue value)
            throw new SpecificationException("Expected scalar value.");

        return Convert.ToString(value.GetValue<object>(), CultureInfo.InvariantCulture);
    }
}
