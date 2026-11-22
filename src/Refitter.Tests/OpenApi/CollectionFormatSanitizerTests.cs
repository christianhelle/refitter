using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class CollectionFormatSanitizerTests
{
    [TestMethod]
    public void Drops_Unrecognised_Value_On_Parameter_And_Items()
    {
        var json = """
            {
              "parameters": [
                {
                  "name": "ids", "in": "query", "type": "array", "collectionFormat": "bogus",
                  "items": { "type": "array", "collectionFormat": "nope", "items": { "type": "string" } }
                }
              ]
            }
            """;

        var parameter = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json))["parameters"]![0]!;

        parameter["collectionFormat"].Should().BeNull();
        parameter["items"]!["collectionFormat"].Should().BeNull();
        parameter["name"]!.Value<string>().Should().Be("ids");
    }

    [TestMethod]
    [DataRow("csv")]
    [DataRow("ssv")]
    [DataRow("tsv")]
    [DataRow("pipes")]
    [DataRow("multi")]
    [DataRow("MULTI")]
    public void Keeps_Recognised_Value(string format)
    {
        var json = $$"""{ "name": "ids", "type": "array", "collectionFormat": "{{format}}" }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Schema_Property_Named_CollectionFormat()
    {
        var json = """{ "properties": { "collectionFormat": { "type": "string" } } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Value_Outside_An_Array_Parameter()
    {
        var json = """{ "example": { "collectionFormat": "bogus" } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    [DataRow("example")]
    [DataRow("examples")]
    [DataRow("default")]
    [DataRow("enum")]
    [DataRow("const")]
    [DataRow("x-sample")]
    public void Keeps_Array_Shaped_Literal_Data(string keyword)
    {
        var json = $$"""{ "schema": { "{{keyword}}": { "type": "array", "collectionFormat": "bogus" } } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }
}
