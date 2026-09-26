namespace Refitter.Tests.OpenApi;

/// <summary>
/// Specs with numeric bounds outside the decimal range, as Swashbuckle emits for [Range(0, double.MaxValue)] (#1273).
/// </summary>
internal static class LargeNumericBoundsSpecs
{
    public const string Yaml = """
        openapi: 3.0.1
        info: { title: Bounds, version: v1 }
        paths: {}
        components:
          schemas:
            M:
              type: object
              properties:
                value: { type: number, format: double, minimum: -1.7976931348623157e308, maximum: 1.7976931348623157e308 }
        """;

    public const string Json = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Bounds", "version": "v1" },
          "paths": {},
          "components": {
            "schemas": {
              "M": {
                "type": "object",
                "properties": {
                  "value": { "type": "number", "format": "double", "minimum": -1.7976931348623157E+308, "maximum": 1.7976931348623157E+308 }
                }
              }
            }
          }
        }
        """;

    public static decimal? GetMaximum(NSwag.OpenApiDocument document) =>
        document.Components.Schemas["M"].Properties["value"].Maximum;

    public static decimal? GetMinimum(NSwag.OpenApiDocument document) =>
        document.Components.Schemas["M"].Properties["value"].Minimum;
}
