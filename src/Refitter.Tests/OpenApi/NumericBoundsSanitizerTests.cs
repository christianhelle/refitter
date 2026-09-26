using AwesomeAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class NumericBoundsSanitizerTests
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
        var json = $$"""{ "type": "number", "{{keyword}}": {{value}} }""";

        var result = Parse(NumericBoundsSanitizer.Sanitize(json));

        result[keyword]!.Value<decimal>().Should().Be(sign > 0 ? decimal.MaxValue : decimal.MinValue);
        result["type"]!.Value<string>().Should().Be("number");
    }

    [Test]
    public void Clamps_Nested_Bounds_And_Keeps_Other_Values()
    {
        const string json = """
            {
              "components": {
                "schemas": {
                  "M": {
                    "properties": {
                      "a": { "type": "number", "minimum": 0, "maximum": 1.7976931348623157E+308 },
                      "b": { "type": "integer", "minimum": -5, "maximum": 255, "exclusiveMinimum": true },
                      "c": { "type": "string", "format": "date-time", "default": "2020-01-01T00:00:00Z", "maximum": null },
                      "d": { "type": "number", "maximum": "unbounded" }
                    }
                  }
                }
              }
            }
            """;

        var result = Parse(NumericBoundsSanitizer.Sanitize(json));
        var properties = result["components"]!["schemas"]!["M"]!["properties"]!;

        properties["a"]!["minimum"]!.Value<int>().Should().Be(0);
        properties["a"]!["maximum"]!.Value<decimal>().Should().Be(decimal.MaxValue);
        properties["b"]!["minimum"]!.Value<int>().Should().Be(-5);
        properties["b"]!["maximum"]!.Value<int>().Should().Be(255);
        properties["b"]!["exclusiveMinimum"]!.Value<bool>().Should().BeTrue();
        properties["c"]!["default"]!.Value<string>().Should().Be("2020-01-01T00:00:00Z");
        properties["c"]!["maximum"]!.Type.Should().Be(JTokenType.Null);
        properties["d"]!["maximum"]!.Value<string>().Should().Be("unbounded");
    }

    [Test]
    public void Does_Not_Change_Description_Text_That_Looks_Like_A_Bound()
    {
        const string json = """
            {
              "description": "maximum: 1e300, then more",
              "maximum": 1e300
            }
            """;

        var result = Parse(NumericBoundsSanitizer.Sanitize(json));

        result["description"]!.Value<string>().Should().Be("maximum: 1e300, then more");
        result["maximum"]!.Value<decimal>().Should().Be(decimal.MaxValue);
    }

    [Test]
    public void Does_Not_Change_Schema_Properties_Named_Like_Bounds()
    {
        const string json = """
            { "properties": { "maximum": { "type": "number", "maximum": 1e300 } } }
            """;

        var result = Parse(NumericBoundsSanitizer.Sanitize(json));

        var maximumProperty = result["properties"]!["maximum"]!;
        maximumProperty["type"]!.Value<string>().Should().Be("number");
        maximumProperty["maximum"]!.Value<decimal>().Should().Be(decimal.MaxValue);
    }

    [Test]
    [Arguments("""{ "maximum": 7.9e28, "minimum": -100 }""")]
    [Arguments("""{ "description": "maximum: 1e300, then more" }""")]
    [Arguments("""{ "maxLength": 1e300 }""")]
    [Arguments("openapi: 3.0.1")]
    [Arguments("\"maximum: 1e300\"")]
    public void Returns_Same_Instance_When_Nothing_To_Clamp(string content)
    {
        NumericBoundsSanitizer.Sanitize(content).Should().BeSameAs(content);
    }

    // NJsonSchema reads bounds as decimals, so parse the same way
    private static JObject Parse(string json) =>
        JsonConvert.DeserializeObject<JObject>(
            json,
            new JsonSerializerSettings { FloatParseHandling = FloatParseHandling.Decimal, DateParseHandling = DateParseHandling.None })!;
}
