namespace Refitter.Core;

/// <summary>
/// The JSON types a schema allows, as flags. The values and their order match the JSON Schema type names
/// Refitter has always generated code for (see THIRD-PARTY-NOTICES.md).
/// </summary>
[Flags]
internal enum ApiObjectType
{
    None = 0,
    Array = 1,
    Boolean = 2,
    Integer = 4,
    Null = 8,
    Number = 16,
    Object = 32,
    String = 64,
    File = 128,
}

/// <summary>The specification an OpenAPI document is written in.</summary>
internal enum ApiSchemaType
{
    JsonSchema,
    Swagger2,
    OpenApi3,
}

internal static class ApiObjectTypeExtensions
{
    public static bool IsNull(this ApiObjectType type) => (type & ApiObjectType.Null) != 0;

    public static bool IsNumber(this ApiObjectType type) => (type & ApiObjectType.Number) != 0;

    public static bool IsObject(this ApiObjectType type) => (type & ApiObjectType.Object) != 0;

    public static bool IsArray(this ApiObjectType type) => (type & ApiObjectType.Array) != 0;

    public static bool IsInteger(this ApiObjectType type) => (type & ApiObjectType.Integer) != 0;

    public static bool IsString(this ApiObjectType type) => (type & ApiObjectType.String) != 0;

    public static bool IsBoolean(this ApiObjectType type) => (type & ApiObjectType.Boolean) != 0;

    public static bool IsFile(this ApiObjectType type) => (type & ApiObjectType.File) != 0;

    public static ApiObjectType Parse(string? value) =>
        value switch
        {
            "array" => ApiObjectType.Array,
            "boolean" => ApiObjectType.Boolean,
            "integer" => ApiObjectType.Integer,
            "number" => ApiObjectType.Number,
            "null" => ApiObjectType.Null,
            "object" => ApiObjectType.Object,
            "string" => ApiObjectType.String,
            "file" => ApiObjectType.File,
            _ => ApiObjectType.None,
        };

    public static string ToJsonName(this ApiObjectType type) => type.ToString().ToLowerInvariant();

    public static readonly ApiObjectType[] AllTypes =
    [
        ApiObjectType.Array,
        ApiObjectType.Boolean,
        ApiObjectType.Integer,
        ApiObjectType.Null,
        ApiObjectType.Number,
        ApiObjectType.Object,
        ApiObjectType.String,
        ApiObjectType.File,
    ];
}
