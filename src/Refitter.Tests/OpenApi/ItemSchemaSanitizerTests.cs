using AwesomeAssertions;
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
}
