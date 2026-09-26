using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class PathItemReferenceInlinerTests
{
    [Test]
    public void Inlines_Path_Items_Referenced_From_Components()
    {
        const string json = """
            {
              "paths": {
                "/items/{id}": { "$ref": "#/components/pathItems/ItemById" },
                "/other": { "get": { "operationId": "GetOther" } },
                "/external": { "$ref": "./other.yaml#/components/pathItems/Other" },
                "/invalid": "not a path item"
              },
              "components": {
                "pathItems": {
                  "ItemById": { "get": { "operationId": "GetItem" } }
                }
              }
            }
            """;

        var result = JObject.Parse(PathItemReferenceInliner.Inline(json));

        result["paths"]!["/items/{id}"]!["$ref"].Should().BeNull();
        result["paths"]!["/items/{id}"]!["get"]!["operationId"]!.Value<string>().Should().Be("GetItem");
        result["paths"]!["/other"]!["get"]!["operationId"]!.Value<string>().Should().Be("GetOther");
        result["paths"]!["/external"]!["$ref"]!.Value<string>().Should().Be("./other.yaml#/components/pathItems/Other");
    }

    [Test]
    public void Keeps_Sibling_Fields_Of_The_Reference()
    {
        const string json = """
            {
              "paths": {
                "/a": { "$ref": "#/components/pathItems/A", "summary": "Overridden" }
              },
              "components": {
                "pathItems": {
                  "A": { "summary": "Original", "description": "Kept", "get": { "operationId": "GetA" } }
                }
              }
            }
            """;

        var path = JObject.Parse(PathItemReferenceInliner.Inline(json))["paths"]!["/a"]!;

        path["summary"]!.Value<string>().Should().Be("Overridden");
        path["description"]!.Value<string>().Should().Be("Kept");
        path["get"]!["operationId"]!.Value<string>().Should().Be("GetA");
    }

    [Test]
    public void Follows_References_Between_Path_Items()
    {
        const string json = """
            {
              "paths": { "/a": { "$ref": "#/components/pathItems/A" } },
              "components": {
                "pathItems": {
                  "A": { "$ref": "#/components/pathItems/B" },
                  "B": { "get": { "operationId": "GetB" } }
                }
              }
            }
            """;

        var path = JObject.Parse(PathItemReferenceInliner.Inline(json))["paths"]!["/a"]!;

        path["$ref"].Should().BeNull();
        path["get"]!["operationId"]!.Value<string>().Should().Be("GetB");
    }

    [Test]
    [Arguments(@"#\/components\/pathItems\/A")]
    [Arguments(@"#/components/pathItems/A")]
    public void Resolves_References_Written_With_Json_Escapes(string reference)
    {
        var json = $$"""
            {
              "paths": { "/a": { "$ref": "{{reference}}" } },
              "components": { "pathItems": { "A": { "get": { "operationId": "GetA" } } } }
            }
            """;

        var path = JObject.Parse(PathItemReferenceInliner.Inline(json))["paths"]!["/a"]!;

        path["get"]!["operationId"]!.Value<string>().Should().Be("GetA");
    }

    [Test]
    public void Resolves_Escaped_Json_Pointer_Names()
    {
        const string json = """
            {
              "paths": { "/a": { "$ref": "#/components/pathItems/a~1b~0c" } },
              "components": { "pathItems": { "a/b~c": { "get": { "operationId": "GetA" } } } }
            }
            """;

        var path = JObject.Parse(PathItemReferenceInliner.Inline(json))["paths"]!["/a"]!;

        path["get"]!["operationId"]!.Value<string>().Should().Be("GetA");
    }

    [Test]
    [Arguments("#/components/pathItems/Missing")]
    [Arguments("#/components/pathItems/Self")]
    public void Leaves_Unresolvable_References_Unchanged(string reference)
    {
        var json = $$"""
            {
              "paths": { "/a": { "$ref": "{{reference}}" } },
              "components": { "pathItems": { "Self": { "$ref": "#/components/pathItems/Self" } } }
            }
            """;

        var path = JObject.Parse(PathItemReferenceInliner.Inline(json))["paths"]!["/a"]!;

        path["$ref"]!.Value<string>().Should().Be(reference);
    }

    [Test]
    [Arguments("""{ "openapi": "3.1.0", "paths": {} }""")]
    [Arguments("""{ "paths": { "/a": { "$ref": "./other.yaml#/pathItems/A" } } }""")]
    [Arguments("""{ "description": "#/components/pathItems/A" }""")]
    [Arguments("""{ "paths": { "/a": { "$ref": "#/components/pathItems/A" } } }""")]
    [Arguments("""["#/components/pathItems/A"]""")]
    public void Returns_Same_Instance_When_Nothing_To_Inline(string json)
    {
        PathItemReferenceInliner.Inline(json).Should().BeSameAs(json);
    }
}
