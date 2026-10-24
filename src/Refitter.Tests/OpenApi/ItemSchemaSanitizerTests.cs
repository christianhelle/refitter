using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class ItemSchemaSanitizerTests
{
    [Test]
    [Arguments("#/info/title/x")]
    [Arguments("#/components/schemas/Payload/allOf/first")]
    [Arguments("#/components/schemas/Payload/allOf/-1")]
    public void Leaves_ItemSchema_With_Unresolvable_Pointer_Untouched(string reference)
    {
        var json = $$"""
            {
              "info": { "title": "Events" },
              "paths": {
                "/": {
                  "get": {
                    "responses": {
                      "200": {
                        "content": {
                          "text/event-stream": {
                            "itemSchema": { "properties": { "data": { "$ref": "{{reference}}" } } }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": { "schemas": { "Payload": { "allOf": [ { "type": "string" } ] } } }
            }
            """;

        ItemSchemaSanitizer.Sanitize(json).Should().Be(json);
    }

    [Test]
    [Arguments("""{ "x-samples": [ { "itemSchema": { "type": "string" } } ] }""")]
    [Arguments("""{ "components": { "schemas": { "Event": { "itemSchema": { "type": "string" } } } } }""")]
    [Arguments("""{ "text/event-stream": { "itemSchema": { "type": "string" } } }""")]
    public void Leaves_ItemSchema_Outside_A_Content_Map_Untouched(string json)
    {
        ItemSchemaSanitizer.Sanitize(json).Should().Be(json);
    }

    // Example values, defaults and extensions are literal JSON that can look like a content map
    [Test]
    [Arguments("example")]
    [Arguments("default")]
    [Arguments("const")]
    [Arguments("x-sample")]
    public void Leaves_Content_Map_Inside_A_Literal_Value_Untouched(string keyword)
    {
        var json = $$"""
            {
              "content": {
                "application/json": {
                  "{{keyword}}": { "content": { "text/event-stream": { "itemSchema": { "type": "string" } } } }
                }
              }
            }
            """;

        ItemSchemaSanitizer.Sanitize(json).Should().Be(json);
    }

    // "default" is also a response code, an entry of the responses map rather than a literal value
    [Test]
    public void Converts_ItemSchema_Of_Default_Response()
    {
        var json = """
            {
              "responses": {
                "default": {
                  "content": { "text/event-stream": { "itemSchema": { "type": "object" } } }
                }
              }
            }
            """;

        var result = JObject.Parse(ItemSchemaSanitizer.Sanitize(json));

        var mediaType = result["responses"]!["default"]!["content"]!["text/event-stream"]!;
        mediaType["itemSchema"].Should().BeNull();
        mediaType["schema"]!["type"]!.Value<string>().Should().Be("array");
    }

    [Test]
    public void Leaves_Content_Map_Inside_A_Named_Example_Value_Untouched()
    {
        var json = """
            {
              "content": {
                "application/json": {
                  "examples": {
                    "stream": {
                      "value": { "content": { "text/event-stream": { "itemSchema": { "type": "string" } } } }
                    }
                  }
                }
              }
            }
            """;

        ItemSchemaSanitizer.Sanitize(json).Should().Be(json);
    }
}
