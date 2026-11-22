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
}
