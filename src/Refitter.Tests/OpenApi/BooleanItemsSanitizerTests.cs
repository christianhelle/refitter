using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class BooleanItemsSanitizerTests
{
    [TestMethod]
    [DataRow("false")]
    [DataRow("true")]
    public void Drops_Boolean_Items_Next_To_PrefixItems(string value)
    {
        var json = $$"""{ "type": "array", "prefixItems": [ { "type": "string" } ], "items": {{value}} }""";

        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json));

        result.ContainsKey("items").Should().BeFalse();
        result["prefixItems"]!.Count().Should().Be(1);
    }

    [TestMethod]
    [DataRow("false")]
    [DataRow("true")]
    public void Drops_Boolean_Written_As_String_By_The_YAML_Conversion(string value)
    {
        var json = OpenApiDocumentParser.ConvertYamlToJson($"type: array\nprefixItems: [ {{ type: string }} ]\nitems: {value}\n");

        JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json)).ContainsKey("items").Should().BeFalse();
    }

    [TestMethod]
    [DataRow("False")]
    [DataRow("FALSE")]
    [DataRow("True")]
    [DataRow("TRUE")]
    public void Drops_Capitalized_YAML_Boolean(string value)
    {
        // YAML 1.2 resolves these spellings to booleans, but the YAML conversion writes them as strings
        var json = OpenApiDocumentParser.ConvertYamlToJson($"type: array\nprefixItems: [ {{ type: string }} ]\nitems: {value}\n");

        JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json)).ContainsKey("items").Should().BeFalse();
    }

    [TestMethod]
    [DataRow("""["array", "null"]""")]
    [DataRow("""["null", "array"]""")]
    public void Drops_Boolean_Items_Of_A_Nullable_Array_Schema(string type)
    {
        var json = $$"""{ "type": {{type}}, "items": false }""";

        JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json)).ContainsKey("items").Should().BeFalse();
    }

    [TestMethod]
    public void Keeps_Boolean_Items_When_The_Type_Array_Has_No_Array()
    {
        var json = """{ "type": ["object", "null"], "items": false }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Drops_Boolean_Items_Of_An_Array_Schema_Without_PrefixItems()
    {
        var json = """{ "type": "array", "items": false }""";

        JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json)).ContainsKey("items").Should().BeFalse();
    }

    [TestMethod]
    public void Keeps_Schema_Items()
    {
        var json = """{ "type": "array", "prefixItems": [ { "type": "string" } ], "items": { "type": "integer" } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Property_Named_Items()
    {
        var json = """{ "properties": { "items": true } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Boolean_Outside_An_Array_Schema()
    {
        var json = """{ "example": { "items": false } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    [DataRow("default")]
    [DataRow("example")]
    [DataRow("const")]
    [DataRow("x-sample")]
    public void Keeps_Boolean_Items_Inside_Literal_Value(string keyword)
    {
        var json = "{ \"components\": { \"schemas\": { \"A\": { \"" + keyword +
            "\": { \"type\": \"array\", \"items\": false } } } } }";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Drops_Boolean_Items_Next_To_Literal_Value()
    {
        var json = """{ "type": "array", "items": false, "default": { "type": "array", "items": false } }""";

        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json));

        result.ContainsKey("items").Should().BeFalse();
        result["default"]!["items"].Should().NotBeNull();
    }
}
