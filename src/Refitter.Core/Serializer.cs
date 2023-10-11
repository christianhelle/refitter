using System.Text.Json;

using JsonNamingPolicy = System.Text.Json.JsonNamingPolicy;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace Refitter.Core;

public static class Serializer
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static T Deserialize<T>(string json) => 
        JsonSerializer.Deserialize<T>(json, JsonSerializerOptions)!;

    public static string Serialize(object any) => 
        JsonSerializer.Serialize(any, JsonSerializerOptions);
}