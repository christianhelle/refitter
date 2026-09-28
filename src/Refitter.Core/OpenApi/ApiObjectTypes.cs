namespace Refitter.Core;

/// <summary>
/// The JSON types a schema allows, as flags. The values and their order match the JSON Schema type names
/// Refitter has always generated code for (see THIRD-PARTY-NOTICES.md).
/// </summary>
[Flags]
internal enum ApiObjectTypes
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
    public static bool IsNull(this ApiObjectTypes type) => (type & ApiObjectTypes.Null) != 0;

    public static bool IsNumber(this ApiObjectTypes type) => (type & ApiObjectTypes.Number) != 0;

    public static bool IsObject(this ApiObjectTypes type) => (type & ApiObjectTypes.Object) != 0;

    public static bool IsArray(this ApiObjectTypes type) => (type & ApiObjectTypes.Array) != 0;

    public static bool IsInteger(this ApiObjectTypes type) => (type & ApiObjectTypes.Integer) != 0;

    public static bool IsString(this ApiObjectTypes type) => (type & ApiObjectTypes.String) != 0;

    public static bool IsBoolean(this ApiObjectTypes type) => (type & ApiObjectTypes.Boolean) != 0;

    public static bool IsFile(this ApiObjectTypes type) => (type & ApiObjectTypes.File) != 0;

    public static ApiObjectTypes Parse(string? value)
    {
        switch (value)
        {
            case "array":
                return ApiObjectTypes.Array;
            case "boolean":
                return ApiObjectTypes.Boolean;
            case "integer":
                return ApiObjectTypes.Integer;
            case "number":
                return ApiObjectTypes.Number;
            case "null":
                return ApiObjectTypes.Null;
            case "object":
                return ApiObjectTypes.Object;
            case "string":
                return ApiObjectTypes.String;
            case "file":
                return ApiObjectTypes.File;
            default:
                return ApiObjectTypes.None;
        }
    }

    public static string ToJsonName(this ApiObjectTypes type) => type.ToString().ToLowerInvariant();

    public static readonly ApiObjectTypes[] AllTypes =
    [
        ApiObjectTypes.Array,
        ApiObjectTypes.Boolean,
        ApiObjectTypes.Integer,
        ApiObjectTypes.Null,
        ApiObjectTypes.Number,
        ApiObjectTypes.Object,
        ApiObjectTypes.String,
        ApiObjectTypes.File,
    ];
}
