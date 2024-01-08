namespace Refitter.Core;

/// <summary>
/// The NSwag IOperationNameGenerator implementation to use
/// </summary> 
public enum OperationNameGeneratorTypes
{
    /// <summary> 
    /// Use the Refitter IOperationNameGenerator implementation
    /// This uses <see cref="MultipleClientsFromOperationId"/> by default but
    /// if there are duplicate operation ID's it is changed to
    /// <see cref="MultipleClientsFromFirstTagAndPathSegments"/>
    /// </summary>
    Default,
    /// <summary>
    /// Generates multiple clients and operation names based
    /// on the Swagger operation ID (underscore separated).
    /// </summary>
    MultipleClientsFromOperationId,
    /// <summary>
    /// Generates the client and operation name based on the path segments
    /// (operation name = last segment, client name = second to last segment).
    /// </summary>
    MultipleClientsFromPathSegments,
    /// <summary>
    /// Generates the client name based on the first tag and
    /// operation name based on the operation id
    /// (operation name = operationId, client name = first tag).
    /// </summary>
    MultipleClientsFromFirstTagAndOperationId,
    /// <summary>
    /// Generates the client name based on the first tag and operation names
    /// based on the Swagger operation ID (underscore separated).
    /// </summary>
    MultipleClientsFromFirstTagAndOperationName,
    /// <summary>
    /// Generates the client name based on the first tag and operation name
    /// based on the path segments (operation name = last segment, client name = first tag).
    /// </summary>
    MultipleClientsFromFirstTagAndPathSegments,
    /// <summary>
    /// Generates the client and operation name
    /// based on the Swagger operation ID.
    /// </summary>
    SingleClientFromOperationId,
    /// <summary>
    /// Generates the operation name from path segments
    /// (suffixed by HTTP operation name if need be)
    /// </summary>
    SingleClientFromPathSegments,
}