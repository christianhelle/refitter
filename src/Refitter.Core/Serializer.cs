using System.Text.Json;

using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Refitter.Core;

/// <summary>
/// Provides methods for serializing and deserializing objects to and from JSON.
/// This serializer is configured to be case-insensitive.
/// </summary>
public static class Serializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Deserializes the JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON string to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type T.</returns>
    public static T Deserialize<T>(string json) => 
        JsonSerializer.Deserialize<T>(json, JsonSerializerOptions)!;

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <param name="any">The object to serialize.</param>
    /// <returns>The JSON string representation of the object.</returns>
    public static string Serialize(object any) => 
        JsonSerializer.Serialize(any, JsonSerializerOptions);
}