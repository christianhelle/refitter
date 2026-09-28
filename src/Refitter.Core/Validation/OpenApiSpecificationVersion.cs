namespace Refitter.Core.Validation;

/// <summary>
/// The version of the OpenAPI specification a document was validated against.
/// </summary>
public enum OpenApiSpecificationVersion
{
    /// <summary>
    /// OpenAPI 2.0, also known as Swagger 2.0.
    /// </summary>
    OpenApi2_0,

    /// <summary>
    /// OpenAPI 3.0.x.
    /// </summary>
    OpenApi3_0,

    /// <summary>
    /// OpenAPI 3.1.x.
    /// </summary>
    OpenApi3_1,

    /// <summary>
    /// OpenAPI 3.2.x.
    /// </summary>
    OpenApi3_2,
}
