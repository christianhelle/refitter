using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class OpenApiJsonNormalizerTests
{
    private const string DocumentNeedingEveryRewrite = """
        {
          "paths": {
            "/events": { "$ref": "#/components/pathItems/Events" }
          },
          "components": {
            "pathItems": {
              "Events": {
                "get": {
                  "operationId": "GetEvents",
                  "responses": {
                    "200": {
                      "description": "OK",
                      "content": {
                        "application/jsonl": { "itemSchema": { "$ref": "#/components/schemas/Status" } }
                      }
                    }
                  }
                }
              }
            },
            "schemas": {
              "Status": {
                "type": "integer",
                "enum": [1, 2],
                "x-enumNames": { "1": "Active", "2": "Inactive" },
                "maximum": 1.7976931348623157E+308
              }
            }
          }
        }
        """;

    [TestMethod]
    public void NormalizeDocument_Applies_Every_Rewrite_In_One_Pass()
    {
        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(DocumentNeedingEveryRewrite));

        var status = result["components"]!["schemas"]!["Status"]!;
        status["maximum"]!.Value<decimal>().Should().Be(decimal.MaxValue);
        status["x-enumNames"]!.Values<string>().Should().Equal("Active", "Inactive");

        var content = result["paths"]!["/events"]!["get"]!["responses"]!["200"]!["content"]!["application/jsonl"]!;
        content["itemSchema"].Should().BeNull();
        content["schema"]!["type"]!.Value<string>().Should().Be("array");
        content["schema"]!["items"]!["$ref"]!.Value<string>().Should().Be("#/components/schemas/Status");
    }

    [TestMethod]
    public void NormalizeReferencedFile_Does_Not_Inline_Path_Items()
    {
        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeReferencedFile(DocumentNeedingEveryRewrite));

        result["paths"]!["/events"]!["$ref"]!.Value<string>().Should().Be("#/components/pathItems/Events");
        result["components"]!["schemas"]!["Status"]!["x-enumNames"]!.Should().BeOfType<JArray>();
    }

    [TestMethod]
    public void NormalizeDocument_Keeps_Numbers_Exact_When_Rewriting()
    {
        const string json = """
            {
              "paths": {
                "/items": { "$ref": "#/components/pathItems/Items" }
              },
              "components": {
                "pathItems": {
                  "Items": { "get": { "operationId": "GetItems" } }
                },
                "schemas": {
                  "Rate": {
                    "type": "number",
                    "enum": [0.12345678901234567890123],
                    "x-enumDescriptions": { "0.12345678901234567890123": "Precise" }
                  }
                }
              }
            }
            """;

        var result = OpenApiJsonNormalizer.NormalizeDocument(json);

        result.Should().Contain("0.12345678901234567890123");
        result.Should().Contain("\"x-enumDescriptions\":[\"Precise\"]");
    }

    [TestMethod]
    [DataRow("""{"openapi":"3.0.0","paths":{}}""")]
    [DataRow("""{"swagger":"2.0","definitions":{"Pet":{"type":"object","maximum":10}}}""")]
    public void Returns_Same_Instance_When_Nothing_To_Rewrite(string json)
    {
        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
        OpenApiJsonNormalizer.NormalizeReferencedFile(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Returns_Same_Instance_When_Candidates_Need_No_Change()
    {
        // The \u escape makes the document a candidate, but nothing in it needs rewriting
        const string json = """{"info":{"title":"Caf\u00e9"},"paths":{}}""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    [DataRow("1e-30")]
    [DataRow("1.5E-40")]
    [DataRow("123456789012345678901234567890.5")]
    public void NormalizeDocument_Keeps_Numbers_Decimal_Cannot_Represent(string number)
    {
        var json = $$"""
            {
              "paths": { "/items": { "$ref": "#/components/pathItems/Items" } },
              "components": {
                "pathItems": { "Items": { "get": { "operationId": "GetItems" } } },
                "schemas": { "Rate": { "type": "number", "default": {{number}} } }
              }
            }
            """;

        OpenApiJsonNormalizer.NormalizeDocument(json)
            .Should()
            .Contain($"\"default\":{number}");
    }

    [TestMethod]
    public void NormalizeDocument_Keeps_Other_Numbers_Exact_When_Clamping_Bounds()
    {
        const string json = """
            {
              "components": {
                "schemas": {
                  "Big": { "type": "number", "maximum": 1.7976931348623157E+308 },
                  "Rate": { "type": "number", "minimum": 0.12345678901234567890123 }
                }
              }
            }
            """;

        var result = OpenApiJsonNormalizer.NormalizeDocument(json);

        result.Should().Contain("\"minimum\":0.12345678901234567890123");
        JObject.Parse(result)["components"]!["schemas"]!["Big"]!["maximum"]!.Value<decimal>()
            .Should()
            .Be(decimal.MaxValue);
    }

    [TestMethod]
    public void NormalizeDocument_Inlines_Path_Items_Spelled_With_Unicode_Escapes()
    {
        const string json = """
            {
              "paths": { "/items": { "$ref": "#/components/path\u0049tems/Items" } },
              "components": {
                "path\u0049tems": { "Items": { "get": { "operationId": "GetItems" } } }
              }
            }
            """;

        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json));

        result["paths"]!["/items"]!["$ref"].Should().BeNull();
        result["paths"]!["/items"]!["get"]!["operationId"]!.Value<string>().Should().Be("GetItems");
    }
}
