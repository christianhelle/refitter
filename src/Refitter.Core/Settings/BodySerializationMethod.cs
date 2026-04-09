namespace Refitter.Core;

/// <summary>
/// Specifies the serialization method to use for body parameters of AnyType
/// </summary>
public enum BodySerializationMethod
{
    /// <summary>
    /// Uses the configured serializer (e.g. System.Text.Json or Newtonsoft.Json)
    /// </summary>
    Serialized,

    /// <summary>
    /// Serializes the body as JSON
    /// </summary>
    Json,

    /// <summary>
    /// URL-encodes the body
    /// </summary>
    UrlEncoded
}
