namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Field names of the OpenAPI specification, and the names of the Microsoft.OpenApi (MIT license) types that its
/// reader errors mention, used more than once by the validator.
/// </summary>
internal static class OpenApiNames
{
    public const string Content = "content";
    public const string Deprecated = "deprecated";
    public const string Description = "description";
    public const string Example = "example";
    public const string Examples = "examples";
    public const string ExternalDocs = "externalDocs";
    public const string Parameters = "parameters";
    public const string Query = "query";
    public const string Required = "required";
    public const string Schema = "schema";
    public const string Summary = "summary";
    public const string XExamples = "x-examples";

    public const string StringType = "String";
    public const string SchemaType = "IOpenApiSchema";
    public const string ExampleType = "IOpenApiExample";
    public const string MediaTypeType = "IOpenApiMediaType";
    public const string EncodingType = "OpenApiEncoding";
}
