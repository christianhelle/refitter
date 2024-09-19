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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Deserializes the JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON string to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Optional custom serialization options</param>
    /// <returns>The deserialized object of type T.</returns>
    public static T Deserialize<T>(string json, JsonSerializerOptions? options = null) => 
        JsonSerializer.Deserialize<T>(json, options ?? JsonSerializerOptions)!;

    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <param name="any">The object to serialize.</param>
    /// <param name="options">Optional custom serialization options</param>
    /// <returns>The JSON string representation of the object.</returns>
    public static string Serialize(object any, JsonSerializerOptions? options = null) => 
        JsonSerializer.Serialize(any, options ?? JsonSerializerOptions);
}