using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// Path items that reference OpenAPI 3.1 <c>components/pathItems</c> are replaced with the referenced path item
/// when a document is read (#1274).
/// </summary>
public class PathItemReferenceTests
{
    [Test]
    public void Inlines_Path_Items_Referenced_From_Components()
    {
        var document = Load("""
            {
              "/items/{id}": { "$ref": "#/components/pathItems/ItemById" },
              "/other": { "get": { "operationId": "GetOther", "responses": {} } }
            }
            """,
            """
            { "ItemById": { "get": { "operationId": "GetItem", "responses": {} } } }
            """);

        document.Paths["/items/{id}"]["get"].OperationId.Should().Be("GetItem");
        document.Paths["/other"]["get"].OperationId.Should().Be("GetOther");
    }

    [Test]
    public void Keeps_Sibling_Fields_Of_The_Reference()
    {
        var document = Load("""
            { "/a": { "$ref": "#/components/pathItems/A", "summary": "Overridden" } }
            """,
            """
            { "A": { "summary": "Original", "description": "Kept", "get": { "operationId": "GetA", "responses": {} } } }
            """);

        var path = document.Paths["/a"];
        path.Summary.Should().Be("Overridden");
        path.Description.Should().Be("Kept");
        path["get"].OperationId.Should().Be("GetA");
    }

    [Test]
    public void Follows_References_Between_Path_Items()
    {
        var document = Load("""
            { "/a": { "$ref": "#/components/pathItems/A" } }
            """,
            """
            {
              "A": { "$ref": "#/components/pathItems/B" },
              "B": { "get": { "operationId": "GetB", "responses": {} } }
            }
            """);

        document.Paths["/a"]["get"].OperationId.Should().Be("GetB");
    }

    [Test]
    [Arguments(@"#\/components\/pathItems\/A")]
    [Arguments(@"#/components/pathItems/A")]
    public void Resolves_References_Written_With_Json_Escapes(string reference)
    {
        var document = Load(
            $$"""{ "/a": { "$ref": "{{reference}}" } }""",
            """{ "A": { "get": { "operationId": "GetA", "responses": {} } } }""");

        document.Paths["/a"]["get"].OperationId.Should().Be("GetA");
    }

    [Test]
    public void Resolves_Escaped_Json_Pointer_Names()
    {
        var document = Load(
            """{ "/a": { "$ref": "#/components/pathItems/a~1b~0c" } }""",
            """{ "a/b~c": { "get": { "operationId": "GetA", "responses": {} } } }""");

        document.Paths["/a"]["get"].OperationId.Should().Be("GetA");
    }

    private static ApiDocument Load(string paths, string pathItems)
    {
        var json = $$"""
            {
              "openapi": "3.1.0",
              "info": { "title": "Path items", "version": "v1" },
              "paths": {{paths}},
              "components": { "pathItems": {{pathItems}} }
            }
            """;

        return ApiDocumentLoader.Load(json, null, isYaml: false);
    }
}
