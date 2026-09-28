using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// Bounds are stored as <see cref="decimal"/>, so bounds outside the decimal range (e.g. <c>double.MaxValue</c>,
/// which Swashbuckle emits for [Range(0, double.MaxValue)]) are clamped to it when a document is read (#1273).
/// </summary>
public class NumericBoundsTests
{
    [Test]
    [Arguments("maximum", "1.7976931348623157e308", 1)]
    [Arguments("maximum", "1.7976931348623157E+308", 1)]
    [Arguments("minimum", "-1.7976931348623157E+308", -1)]
    [Arguments("exclusiveMaximum", "8e28", 1)]
    [Arguments("exclusiveMinimum", "-8e28", -1)]
    [Arguments("maximum", "100000000000000000000000000000000", 1)]
    [Arguments("maximum", "7.922816251426434e28", 1)]
    [Arguments("maximum", "\"1.7976931348623157e308\"", 1)]
    public void Clamps_Bounds_Outside_Decimal_Range(string keyword, string value, int sign)
    {
        var schema = LoadSchema($$"""{ "type": "number", "{{keyword}}": {{value}} }""");

        var bound = keyword switch
        {
            "maximum" => schema.Maximum,
            "minimum" => schema.Minimum,
            "exclusiveMaximum" => schema.ExclusiveMaximum,
            _ => schema.ExclusiveMinimum,
        };
        bound.Should().Be(sign > 0 ? decimal.MaxValue : decimal.MinValue);
        schema.Type.Should().Be(ApiObjectTypes.Number);
    }

    [Test]
    public void Clamps_Nested_Bounds_And_Keeps_Other_Values()
    {
        var schema = LoadSchema("""
            {
              "type": "object",
              "properties": {
                "a": { "type": "number", "minimum": 0, "maximum": 1.7976931348623157E+308 },
                "b": { "type": "integer", "minimum": -5, "maximum": 255, "exclusiveMinimum": true },
                "c": { "type": "string", "format": "date-time", "default": "2020-01-01T00:00:00Z", "maximum": null }
              }
            }
            """);

        schema.Properties["a"].Minimum.Should().Be(0);
        schema.Properties["a"].Maximum.Should().Be(decimal.MaxValue);
        schema.Properties["b"].Minimum.Should().Be(-5);
        schema.Properties["b"].Maximum.Should().Be(255);
        schema.Properties["b"].IsExclusiveMinimum.Should().BeTrue();
        schema.Properties["c"].Default.Should().NotBeNull();
        schema.Properties["c"].Maximum.Should().BeNull();
    }

    [Test]
    public void Does_Not_Change_Description_Text_That_Looks_Like_A_Bound()
    {
        var schema = LoadSchema("""
            {
              "description": "maximum: 1e300, then more",
              "maximum": 1e300
            }
            """);

        schema.Description.Should().Be("maximum: 1e300, then more");
        schema.Maximum.Should().Be(decimal.MaxValue);
    }

    [Test]
    public void Does_Not_Change_Schema_Properties_Named_Like_Bounds()
    {
        var schema = LoadSchema("""
            { "properties": { "maximum": { "type": "number", "maximum": 1e300 } } }
            """);

        var maximumProperty = schema.Properties["maximum"];
        maximumProperty.Type.Should().Be(ApiObjectTypes.Number);
        maximumProperty.Maximum.Should().Be(decimal.MaxValue);
    }

    [Test]
    public void Keeps_Bounds_Inside_Decimal_Range()
    {
        var schema = LoadSchema("""{ "maximum": 7.9e28, "minimum": -100 }""");

        schema.Maximum.Should().Be(79000000000000000000000000000m);
        schema.Minimum.Should().Be(-100);
    }

    private static ApiSchema LoadSchema(string schema)
    {
        var json = $$"""
            {
              "openapi": "3.0.1",
              "info": { "title": "Bounds", "version": "v1" },
              "paths": {},
              "components": { "schemas": { "S": {{schema}} } }
            }
            """;

        return ApiDocumentLoader.Load(json, null, isYaml: false).Components.Schemas["S"];
    }
}
