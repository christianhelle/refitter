using System.ComponentModel;

namespace Refitter.Core;

/// <summary>
/// The NSwag IOperationNameGenerator implementation to use
/// </summary>
[Description("The NSwag IOperationNameGenerator implementation to use")]
public enum OperationNameGeneratorTypes
{
    /// <summary>
    /// Use the Refitter IOperationNameGenerator implementation
    /// This uses <see cref="MultipleClientsFromOperationId"/> by default but
    /// if there are duplicate operation ID's it is changed to
    /// <see cref="MultipleClientsFromFirstTagAndPathSegments"/>
    /// </summary>
    [Description(
        """
        Use the Refitter IOperationNameGenerator implementation
        This uses MultipleClientsFromOperationId by default but
        if there are duplicate operation ID's it is changed to
        MultipleClientsFromFirstTagAndPathSegments
        """
    )]
    Default,
    /// <summary>
    /// Generates multiple clients and operation names based
    /// on the Swagger operation ID (underscore separated).
    /// </summary>
    [Description(
        """
        Generates multiple clients and operation names based
        on the Swagger operation ID (underscore separated).
        """
    )]
    MultipleClientsFromOperationId,
    /// <summary>
    /// Generates the client and operation name based on the path segments
    /// (operation name = last segment, client name = second to last segment).
    /// </summary>
    [Description(
        """
        Generates the client and operation name based on the path segments
        (operation name = last segment, client name = second to last segment).
        """
    )]
    MultipleClientsFromPathSegments,
    /// <summary>
    /// Generates the client name based on the first tag and
    /// operation name based on the operation id
    /// (operation name = operationId, client name = first tag).
    /// </summary>
    [Description(
        """
        Generates the client name based on the first tag and
        operation name based on the operation id
        (operation name = operationId, client name = first tag).
        """
    )]
    MultipleClientsFromFirstTagAndOperationId,
    /// <summary>
    /// Generates the client name based on the first tag and operation names
    /// based on the Swagger operation ID (underscore separated).
    /// </summary>
    [Description(
        """
        Generates the client name based on the first tag and operation names
        based on the Swagger operation ID (underscore separated).
        """
    )]
    MultipleClientsFromFirstTagAndOperationName,
    /// <summary>
    /// Generates the client name based on the first tag and operation name
    /// based on the path segments (operation name = last segment, client name = first tag).
    /// </summary>
    [Description(
        """
        Generates the client name based on the first tag and operation name
        based on the path segments (operation name = last segment, client name = first tag).
        """
    )]
    MultipleClientsFromFirstTagAndPathSegments,
    /// <summary>
    /// Generates the client and operation name
    /// based on the Swagger operation ID.
    /// </summary>
    [Description(
        """
        Generates the client and operation name
        based on the Swagger operation ID.
        """
    )]
    SingleClientFromOperationId,
    /// <summary>
    /// Generates the operation name from path segments
    /// (suffixed by HTTP operation name if need be)
    /// </summary>
    [Description(
        """
        Generates the operation name from path segments
        (suffixed by HTTP operation name if need be)
        """
    )]
    SingleClientFromPathSegments,
}
