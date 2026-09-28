using System.Text.Json;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

public class ApiReferenceResolverTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    private static ApiDocument LoadFiles(string main, params (string Name, string Content)[] others)
    {
        var folder = TestDirectory.CreateFolder();
        foreach (var (name, content) in others)
            File.WriteAllText(Path.Combine(folder, name), content);

        var path = Path.Combine(folder, "main.json");
        File.WriteAllText(path, main);
        return ApiDocumentLoader.LoadFile(path);
    }

    private static ApiDocument LoadWithUrls(string json, string? documentPath, Dictionary<string, string> urls)
    {
        using var element = JsonDocument.Parse(json);
        var document = new ApiJsonReader(ApiSchemaType.OpenApi3).ReadDocument(element.RootElement);
        document.DocumentPath = documentPath;
        new ApiReferenceResolver(document, url => urls[url]).Resolve();
        return document;
    }

    private static ApiSchema ResponseSchema(ApiDocument document, string path = "/a", string method = "get") =>
        document.Paths[path][method].Responses["200"].ActualResponse.Content["application/json"].Schema!;

    private static readonly string Components = """
        "components": {
          "schemas": {
            "Pet": {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "owner": { "$ref": "#/components/schemas/Owner" },
                "tags": { "type": "array", "items": { "type": "string", "format": "tag" } },
                "extra": { "additionalProperties": { "type": "integer", "format": "extra" } },
                "choice": { "allOf": [{ "type": "number", "format": "choice" }] }
              },
              "patternProperties": { "^x-": { "type": "boolean", "format": "pattern" } },
              "definitions": { "Inner": { "type": "string", "format": "inner" } },
              "discriminator": { "propertyName": "kind", "mapping": { "cat": "#/components/schemas/Owner" } },
              "x-custom": { "type": "string", "format": "custom" }
            },
            "Owner": { "type": "object", "properties": { "id": { "type": "integer", "format": "owner-id" } } },
            "Tuple": { "type": "array", "items": [{ "type": "string", "format": "first" }] },
            "ThroughReference": { "allOf": [{ "$ref": "#/components/schemas/Pet/properties/owner/properties/id" }] }
          },
          "headers": { "Rate": { "$ref": "#/components/schemas/Owner" } },
          "securitySchemes": { "key": { "type": "apiKey", "name": "k", "in": "header" } }
        }
        """;

    private static string Document(string reference) => $$"""
        {
          "openapi": "3.0.1",
          "info": { "title": "T", "version": "1" },
          "paths": {
            "/a": {
              "parameters": [{ "name": "shared", "in": "query", "schema": { "type": "string", "format": "shared" } }],
              "get": {
                "parameters": [{ "name": "id", "in": "path", "schema": { "type": "string", "format": "id" } }],
                "requestBody": { "content": { "application/json": { "schema": { "type": "string", "format": "body" } } } },
                "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "{{reference}}" } } } } }
              }
            }
          },
          {{Components}}
        }
        """;

    [Test]
    [Arguments("#/components/schemas/Pet/properties/name", "string", null)]
    [Arguments("#/components/schemas/Pet/properties/tags/items", "string", "tag")]
    [Arguments("#/components/schemas/Pet/properties/extra/additionalProperties", "integer", "extra")]
    [Arguments("#/components/schemas/Pet/properties/choice/allOf/0", "number", "choice")]
    [Arguments("#/components/schemas/Pet/patternProperties/%5Ex-", "boolean", "pattern")]
    [Arguments("#/components/schemas/Pet/definitions/Inner", "string", "inner")]
    [Arguments("#/components/schemas/Pet/x-custom", "string", "custom")]
    [Arguments("#/components/schemas/Tuple/items/0", "string", "first")]
    [Arguments("#/paths/~1a/get/parameters/0/schema", "string", "id")]
    [Arguments("#/paths/~1a/parameters/0/schema", "string", "shared")]
    [Arguments("#/paths/~1a/get/requestBody/content/application~1json/schema", "string", "body")]
    public void Resolves_Pointers_Into_The_Document(string reference, string type, string? format)
    {
        var schema = ResponseSchema(Load(Document(reference))).ActualSchema;

        schema.Type.Should().Be(ApiObjectTypeExtensions.Parse(type));
        schema.Format.Should().Be(format);
    }

    [Test]
    public void Resolves_Pointers_Through_References()
    {
        // Pet's properties are resolved before this schema, so the pointer follows owner's reference to Owner
        var document = Load(Document("#/components/schemas/Owner"));

        document.Definitions["ThroughReference"].AllOf[0].ActualSchema.Format.Should().Be("owner-id");
    }

    [Test]
    public void Resolves_Pointers_Into_Discriminator_Mappings()
    {
        var schema = ResponseSchema(Load(Document("#/components/schemas/Pet/discriminator/mapping/cat"))).ActualSchema;

        schema.Properties.Keys.Should().Equal("id");
    }

    [Test]
    public void Resolves_Headers_In_Components()
    {
        var document = Load(Document("#/components/schemas/Owner"));

        document.Components.Headers["Rate"].Reference.Should().BeSameAs(document.Components.Schemas["Owner"]);
    }

    [Test]
    [Arguments("#/components/schemas/Missing")]
    [Arguments("#/components/unknown/Pet")]
    [Arguments("#/components/schemas/Pet/properties/missing")]
    [Arguments("#/components/schemas/Pet/unknown")]
    [Arguments("#/components/schemas/Tuple/items/5")]
    [Arguments("#/paths/~1a/get/unknown")]
    [Arguments("#/paths/~1a/get/requestBody/unknown")]
    [Arguments("#/paths/~1a/get/requestBody/content/text~1plain")]
    [Arguments("#/paths/~1a/get/responses/200/content/application~1json/example")]
    [Arguments("#/components/schemas/Pet/discriminator/unknown")]
    [Arguments("#/paths/~1a/get/parameters/x")]
    [Arguments("#/info/title")]
    public void Throws_For_Pointers_That_Cannot_Be_Resolved(string reference)
    {
        var act = () => Load(Document(reference));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Could not resolve the path*");
    }

    [Test]
    public void Resolves_Pointers_Into_Responses()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "responses": {
                      "200": { "description": "ok", "headers": { "X-Rate": { "schema": { "type": "integer" } } }, "x-shape": { "type": "string", "format": "shape" } },
                      "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/paths/~1a/get/responses/200/headers/X-Rate/schema" } } } },
                      "202": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/paths/~1a/get/responses/200/x-shape" } } } }
                    }
                  }
                }
              }
            }
            """);

        var responses = document.Paths["/a"]["get"].Responses;
        responses["201"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Integer);
        responses["202"].Content["application/json"].Schema!.ActualSchema.Format.Should().Be("shape");
    }

    private static readonly string EveryObject = """
        {
          "openapi": "3.0.1",
          "info": { "title": "T", "version": "1" },
          "tags": [{ "name": "pets" }],
          "x-raw": { "list": [{ "type": "string", "format": "raw-item" }], "text": "value" },
          "paths": {
            "/a": {
              "parameters": [{ "name": "p", "in": "query", "schema": { "type": "string", "format": "path-parameter" } }],
              "x-path": { "type": "string", "format": "path-extension" },
              "get": {
                "parameters": [{ "name": "q", "in": "query", "x-schema": { "type": "string", "format": "custom" }, "schema": { "type": "string" } }],
                "requestBody": { "$ref": "#/components/requestBodies/Body" },
                "responses": {
                  "200": { "$ref": "#/components/responses/Ok" },
                  "201": { "description": "ok", "headers": { "X": { "schema": { "type": "string", "format": "header" } } }, "x-response": { "type": "string", "format": "response-extension" } }
                }
              }
            },
            "/b": { "$ref": "#/paths/~1a" }
          },
          "components": {
            "schemas": {
              "Owner": { "type": "object", "properties": { "id": { "type": "integer" } } },
              "Pet": {
                "type": "object",
                "properties": { "owner": { "$ref": "#/components/schemas/Owner", "x-extra": { "type": "string", "format": "extra" } } },
                "anyOf": [{ "type": "string", "format": "any" }],
                "oneOf": [{ "type": "string", "format": "one" }],
                "not": { "type": "string", "format": "not" },
                "x-dictionaryKey": { "type": "string", "format": "key" },
                "additionalItems": { "type": "string", "format": "additional-item" }
              },
              "Empty": { "type": "object" }
            },
            "requestBodies": { "Body": { "content": { "application/json": { "schema": { "type": "string", "format": "request-body" } } } } },
            "responses": { "Ok": { "description": "ok", "content": { "application/json": { "schema": { "type": "string", "format": "response" } } } } },
            "parameters": { "Id": { "name": "id", "in": "query", "schema": { "type": "string", "format": "component-parameter" } } },
            "securitySchemes": { "key": { "type": "apiKey", "name": "k", "in": "header" } }
          }
        }
        """;

    [Test]
    [Arguments("#/paths/~1a/parameters/0/schema", "path-parameter")]
    [Arguments("#/paths/~1a/x-path", "path-extension")]
    [Arguments("#/paths/~1a/get/parameters/0/x-schema", "custom")]
    [Arguments("#/paths/~1a/get/requestBody/content/application~1json/schema", "request-body")]
    [Arguments("#/paths/~1a/get/responses/200/content/application~1json/schema", "response")]
    [Arguments("#/paths/~1a/get/responses/201/headers/X/schema", "header")]
    [Arguments("#/paths/~1a/get/responses/201/x-response", "response-extension")]
    [Arguments("#/paths/~1b/get/responses/201/headers/X/schema", "header")]
    [Arguments("#/components/parameters/Id/schema", "component-parameter")]
    [Arguments("#/components/schemas/Pet/properties/owner/x-extra", "extra")]
    [Arguments("#/components/schemas/Pet/anyOf/0", "any")]
    [Arguments("#/components/schemas/Pet/oneOf/0", "one")]
    [Arguments("#/components/schemas/Pet/not", "not")]
    [Arguments("#/components/schemas/Pet/x-dictionaryKey", "key")]
    [Arguments("#/components/schemas/Pet/additionalItems", "additional-item")]
    [Arguments("#/definitions/Pet/not", "not")]
    [Arguments("#/x-raw/list/0", "raw-item")]
    public void Resolves_Pointers_Through_Every_Kind_Of_Object(string pointer, string format)
    {
        var json = EveryObject.Replace("\"Empty\": { \"type\": \"object\" }", "\"Empty\": { \"type\": \"object\" }, \"Target\": { \"allOf\": [{ \"$ref\": \"" + pointer + "\" }] }");

        var document = Load(json);

        document.Definitions["Target"].AllOf[0].ActualSchema.Format.Should().Be(format);
    }

    [Test]
    [Arguments("#/components/schemas/Empty/properties/a")]
    [Arguments("#/components/schemas/Empty/patternProperties/a")]
    [Arguments("#/components/schemas/Empty/definitions/a")]
    [Arguments("#/components/schemas/Empty/items")]
    [Arguments("#/components/schemas/Empty/allOf/0")]
    [Arguments("#/components/schemas/Empty/anyOf/0")]
    [Arguments("#/components/schemas/Empty/oneOf/0")]
    [Arguments("#/components/schemas/Empty/discriminator")]
    [Arguments("#/components/parameters/Id/x-schema")]
    [Arguments("#/components/unknown")]
    [Arguments("#/paths/~1a/get/responses/201/unknown")]
    [Arguments("#/paths/~1a/unknown")]
    [Arguments("#/x-raw/missing")]
    [Arguments("#/x-raw/list/5")]
    [Arguments("#/x-raw/list/first")]
    [Arguments("#/x-raw/text/value")]
    [Arguments("#/tags/0/name")]
    [Arguments("#/components/responses/Missing")]
    public void Does_Not_Resolve_Pointers_To_Missing_Objects(string pointer)
    {
        var json = EveryObject.Replace("\"Empty\": { \"type\": \"object\" }", "\"Empty\": { \"type\": \"object\" }, \"Target\": { \"allOf\": [{ \"$ref\": \"" + pointer + "\" }] }");

        FluentActions.Invoking(() => Load(json)).Should().Throw<InvalidOperationException>().WithMessage("*Could not resolve the path*");
    }

    [Test]
    [Arguments("#")]
    [Arguments("#/securityDefinitions/key")]
    [Arguments("#/components/securitySchemes/key")]
    [Arguments("#/tags/0")]
    [Arguments("#/paths/~1a")]
    public void Rejects_Pointers_To_Objects_That_Are_Not_Schemas(string pointer)
    {
        var json = EveryObject.Replace("\"Empty\": { \"type\": \"object\" }", "\"Empty\": { \"type\": \"object\" }, \"Target\": { \"allOf\": [{ \"$ref\": \"" + pointer + "\" }] }");

        FluentActions.Invoking(() => Load(json)).Should().Throw<Exception>();
    }

    [Test]
    public void Resolves_Swagger2_Pointers()
    {
        var document = Load("""
            {
              "swagger": "2.0",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "parameters": [{ "$ref": "#/parameters/Id" }],
                    "responses": {
                      "200": { "$ref": "#/responses/Ok" },
                      "201": { "description": "ok", "schema": { "$ref": "#/paths/~1a/get/responses/202/schema" } },
                      "202": { "description": "ok", "schema": { "type": "string", "format": "inline" } }
                    }
                  }
                }
              },
              "parameters": { "Id": { "name": "id", "in": "query", "type": "string" } },
              "responses": { "Ok": { "description": "ok", "schema": { "$ref": "#/definitions/Pet" } } },
              "definitions": { "Pet": { "type": "object" } }
            }
            """);

        var operation = document.Paths["/a"]["get"];
        operation.Parameters[0].ActualParameter.Name.Should().Be("id");
        operation.Responses["200"].ActualResponse.Schema!.ActualSchema.Should().BeSameAs(document.Definitions["Pet"]);
        operation.Responses["201"].Schema!.ActualSchema.Format.Should().Be("inline");
    }

    [Test]
    public void Reads_Referenced_Extension_Data_As_The_Referenced_Kind_Of_Object()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "x-schemas": { "Name": { "type": "string", "format": "name" } },
              "x-parameters": { "Id": { "name": "id", "in": "query", "schema": { "type": "string" } } },
              "x-bodies": { "Body": { "content": { "application/json": { "schema": { "type": "integer" } } } } },
              "x-paths": { "B": { "post": { "responses": { "200": { "description": "ok" } } } } },
              "x-list": [{ "type": "boolean" }],
              "paths": {
                "/a": {
                  "get": {
                    "x-local": { "type": "number" },
                    "parameters": [{ "$ref": "#/x-parameters/Id" }],
                    "requestBody": { "$ref": "#/x-bodies/Body" },
                    "responses": {
                      "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/x-schemas/Name" } } } },
                      "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/paths/~1a/get/x-local" } } } },
                      "202": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/x-list/0" } } } }
                    }
                  }
                },
                "/b": { "$ref": "#/x-paths/B" }
              }
            }
            """);

        var operation = document.Paths["/a"]["get"];
        operation.Parameters[0].ActualParameter.Name.Should().Be("id");
        operation.RequestBody!.ActualRequestBody.Content["application/json"].Schema!.Type.Should().Be(ApiObjectTypes.Integer);
        operation.Responses["200"].Content["application/json"].Schema!.ActualSchema.Format.Should().Be("name");
        operation.Responses["201"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Number);
        operation.Responses["202"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Boolean);
        document.Paths["/b"].ActualPathItem.Keys.Should().Equal("post");
    }

    [Test]
    public void Resolves_References_To_Other_Files()
    {
        var document = LoadFiles(
            """
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "parameters": [{ "$ref": "parameters.json#/Id" }],
                    "responses": {
                      "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "schemas.json#/definitions/Pet" } } } },
                      "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "schemas.json#/definitions/Pet" } } } },
                      "202": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "list.json#/0" } } } },
                      "203": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "whole.yaml" } } } }
                    }
                  }
                }
              }
            }
            """,
            ("parameters.json", """{ "Id": { "name": "id", "in": "query", "schema": { "type": "string", "x-schema": { "type": "string" } } } }"""),
            ("schemas.json", """
                {
                  "definitions": {
                    "Pet": {
                      "type": "object",
                      "properties": {
                        "owner": { "$ref": "#/definitions/Owner" },
                        "tags": { "type": "array", "items": { "$ref": "other.json#/Tag" } },
                        "tuple": { "items": [{ "type": "string" }], "additionalItems": { "type": "string" } },
                        "map": { "additionalProperties": { "type": "string" } },
                        "mixed": { "allOf": [{ "type": "string" }], "anyOf": [{ "type": "string" }], "oneOf": [{ "type": "string" }], "not": { "type": "null" } },
                        "keyed": { "x-dictionaryKey": { "type": "string" } }
                      },
                      "patternProperties": { "^x": { "type": "string" } },
                      "definitions": { "Local": { "type": "string" } },
                      "discriminator": { "propertyName": "kind", "mapping": { "owner": "#/definitions/Owner" } }
                    },
                    "Owner": { "type": "object" }
                  }
                }
                """),
            ("other.json", """{ "Tag": { "type": "string" } }"""),
            ("list.json", """[{ "type": "boolean" }]"""),
            ("whole.yaml", "type: integer\nformat: int64\n"));

        var responses = document.Paths["/a"]["get"].Responses;
        var pet = responses["200"].Content["application/json"].Schema!.ActualSchema;
        pet.Should().BeSameAs(responses["201"].Content["application/json"].Schema!.ActualSchema);
        pet.Properties["owner"].ActualSchema.Type.Should().Be(ApiObjectTypes.Object);
        pet.Properties["tags"].Item!.ActualSchema.Type.Should().Be(ApiObjectTypes.String);
        responses["202"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Boolean);
        var whole = responses["203"].Content["application/json"].Schema!.ActualSchema;
        whole.Format.Should().Be("int64");
        whole.DocumentPath.Should().EndWith("whole.yaml");
        document.Paths["/a"]["get"].Parameters[0].ActualParameter.Name.Should().Be("id");
        document.Definitions.Keys.Should().Contain(["Pet", "Owner", "Tag"]);
    }

    [Test]
    [Arguments("schemas.json#/definitions/Missing", "Could not resolve the JSON path")]
    [Arguments("missing.json#/Pet", "Could not resolve the JSON path")]
    public void Throws_For_References_To_Other_Files_That_Cannot_Be_Resolved(string reference, string message)
    {
        var act = () => LoadFiles(
            Document(reference),
            ("schemas.json", """{ "definitions": {} }"""));

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{message}*");
    }

    [Test]
    public void Does_Not_Support_Responses_From_Other_Files()
    {
        var act = () => LoadFiles(
            """
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": { "/a": { "get": { "responses": { "200": { "$ref": "responses.json#/Ok" } } } } }
            }
            """,
            ("responses.json", """{ "Ok": { "description": "ok" } }"""));

        act.Should().Throw<InvalidOperationException>().WithInnerException<NotSupportedException>();
    }

    [Test]
    public void Resolves_Urls_Absolute_And_Relative_To_The_Document_Url()
    {
        var urls = new Dictionary<string, string>
        {
            ["https://example.com/specs/schemas.json"] = """{ "Pet": { "type": "object" } }""",
            ["https://example.com/other/tag.json"] = """{ "type": "string" }""",
        };

        var document = LoadWithUrls(
            """
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "responses": {
                      "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "schemas.json#/Pet" } } } },
                      "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "https://example.com/other/tag.json" } } } }
                    }
                  }
                }
              }
            }
            """,
            "https://example.com/specs/openapi.json",
            urls);

        var responses = document.Paths["/a"]["get"].Responses;
        responses["200"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Object);
        responses["201"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.String);
    }

    [Test]
    public void Cannot_Resolve_Relative_References_Without_A_Document_Path()
    {
        var act = () => Load(Document("schemas.json#/Pet"));

        act.Should().Throw<NotSupportedException>().WithMessage("*no document path*");
    }
}
