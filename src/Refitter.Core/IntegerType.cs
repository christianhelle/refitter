namespace Refitter.Core;

/// <summary>
/// Specifies the .NET type to use for OpenAPI integer types
/// without a format specifier
/// </summary>
public enum IntegerType
{
    /// <summary>
    /// Use System.Int32 (int) for integers without format
    /// </summary>
    Int32,

    /// <summary>
    /// Use System.Int64 (long) for integers without format
    /// </summary>
    Int64
}
