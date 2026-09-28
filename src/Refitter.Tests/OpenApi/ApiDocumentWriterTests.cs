using System.Collections;
using System.Globalization;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class ApiDocumentWriterTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    private static readonly string OpenApiDocument = """
        {
          "openapi": "3.0.1",
          "info": { "title": "T", "version": "1" },
          "paths": {
            "/a": {
              "post": {
                "parameters": [
                  { "name": "q", "in": "query", "style": "matrix", "x-position": 2, "x-schema": { "type": "string" }, "schema": { "type": "string" } },
                  { "name": "l", "in": "cookie", "style": "label" },
                  { "name": "m", "in": "modelbinding" }
                ],
                "requestBody": { "x-name": "payload", "x-position": 1, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } },
                "responses": {
                  "200": { "description": null, "headers": { "X-Rate": { "$ref": "#/components/headers/Rate" } } },
                  "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet/properties/tuple" } } } }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Pet": {
                "type": "object",
                "maxProperties": 5,
                "minProperties": 1,
                "deprecated": true,
                "x-abstract": true,
                "discriminator": { "propertyName": "kind", "mapping": { "cat": "#/components/schemas/Cat" } },
                "properties": {
                  "flags": { "type": "integer", "enum": [1, 2], "x-enumFlags": true, "x-enum-descriptions": ["one", null] },
                  "map": { "type": "object", "x-dictionaryKey": { "type": "string" }, "additionalProperties": { "type": "string" } },
                  "tuple": { "type": "array", "items": [{ "type": "string" }], "additionalItems": { "type": "integer" } },
                  "closed": { "type": "array", "items": [{ "type": "string" }], "additionalItems": false },
                  "secret": { "type": "string", "writeOnly": true }
                },
                "patternProperties": { "^x-": { "type": "string" } },
                "definitions": { "Inner": { "type": "string" } }
              },
              "Cat": { "allOf": [{ "$ref": "#/components/schemas/Pet" }, { "type": "object", "properties": { "lives": { "type": "integer" } } }] }
            },
            "headers": { "Rate": { "schema": { "type": "integer" } } },
            "securitySchemes": {
              "oidc": { "type": "openIdConnect", "openIdConnectUrl": "https://example.com" },
              "basic": { "type": "http", "scheme": "basic" }
            }
          }
        }
        """;

    [Test]
    public void Copies_OpenApi3_Documents_With_All_Keywords()
    {
        var copy = ApiDocumentWriter.Clone(Load(OpenApiDocument));

        var operation = copy.Paths["/a"]["post"];
        var query = operation.Parameters.Single(p => p.Name == "q");
        query.Style.Should().Be(ApiParameterStyle.Matrix);
        query.Position.Should().Be(2);
        query.CustomSchema!.Type.Should().Be(ApiObjectTypes.String);
        operation.Parameters.Single(p => p.Name == "l").Style.Should().Be(ApiParameterStyle.Label);
        operation.Parameters.Single(p => p.Name == "m").Kind.Should().Be(ApiParameterKind.ModelBinding);
        operation.RequestBody!.Position.Should().Be(1);
        operation.Responses["200"].Description.Should().BeEmpty();
        operation.Responses["200"].Headers["X-Rate"].ActualSchema.Type.Should().Be(ApiObjectTypes.Integer);

        var pet = copy.Definitions["Pet"];
        pet.MaxProperties.Should().Be(5);
        pet.MinProperties.Should().Be(1);
        pet.IsDeprecated.Should().BeTrue();
        pet.IsAbstract.Should().BeTrue();
        pet.DiscriminatorObject!.Mapping["cat"].ActualSchema.Should().BeSameAs(copy.Definitions["Cat"]);
        pet.Properties["flags"].IsFlagEnumerable.Should().BeTrue();
        pet.Properties["flags"].EnumerationDescriptions.Should().Equal("one", null);
        pet.Properties["map"].DictionaryKey!.Type.Should().Be(ApiObjectTypes.String);
        pet.Properties["tuple"].AdditionalItemsSchema!.Type.Should().Be(ApiObjectTypes.Integer);
        pet.Properties["closed"].AllowAdditionalItems.Should().BeFalse();
        pet.Properties["secret"].IsWriteOnly.Should().BeTrue();
        pet.PatternProperties.Keys.Should().Equal("^x-");
        pet.Definitions.Keys.Should().Equal("Inner");
        operation.Responses["201"].Content["application/json"].Schema!.ActualSchema.Type.Should().Be(ApiObjectTypes.Array);
        copy.Components.SecuritySchemes["oidc"].Type.Should().Be(ApiSecuritySchemeType.OpenIdConnect);
        copy.Components.SecuritySchemes["basic"].Scheme.Should().Be("basic");
    }

    [Test]
    public void Copies_Swagger2_Documents_With_All_Keywords()
    {
        var document = Load("""
            {
              "swagger": "2.0",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "parameters": [{ "name": "id", "in": "path", "type": "string", "x-schema": { "type": "string" } }],
                    "responses": {
                      "200": { "$ref": "#/responses/Ok" },
                      "201": { "description": "ok", "x-nullable": true, "schema": { "$ref": "#/responses/Ok/schema" } }
                    }
                  }
                }
              },
              "responses": { "Ok": { "description": "ok", "schema": { "$ref": "#/definitions/Pet" } } },
              "definitions": {
                "Pet": {
                  "type": "object",
                  "discriminator": "kind",
                  "x-deprecated": true,
                  "properties": { "kind": { "type": "string" }, "secret": { "type": "string", "x-writeOnly": true } }
                }
              },
              "securityDefinitions": {
                "basic": { "type": "basic" },
                "oauth": { "type": "oauth2" }
              }
            }
            """);
        document.SecurityDefinitions["oidc"] = new ApiSecurityScheme { Type = ApiSecuritySchemeType.OpenIdConnect };
        document.SecurityDefinitions["unknown"] = new ApiSecurityScheme();

        var copy = ApiDocumentWriter.Clone(document);

        var operation = copy.Paths["/a"]["get"];
        operation.Parameters[0].CustomSchema!.Type.Should().Be(ApiObjectTypes.String);
        operation.Responses["200"].ActualResponse.Schema!.ActualSchema.Should().BeSameAs(copy.Definitions["Pet"]);
        operation.Responses["201"].IsNullableRaw.Should().BeTrue();
        var pet = copy.Definitions["Pet"];
        pet.Discriminator.Should().Be("kind");
        pet.IsDeprecated.Should().BeTrue();
        pet.Properties["secret"].IsWriteOnly.Should().BeTrue();
        copy.SecurityDefinitions["basic"].Type.Should().Be(ApiSecuritySchemeType.Http);
        copy.SecurityDefinitions["oidc"].Type.Should().Be(ApiSecuritySchemeType.OAuth2);
        copy.SecurityDefinitions["unknown"].Type.Should().Be(ApiSecuritySchemeType.Undefined);
    }

    [Test]
    public void Writes_Discriminator_Mappings_That_Were_Not_Resolved()
    {
        var document = Load("""{ "openapi": "3.0.1", "info": { "title": "T", "version": "1" }, "paths": {} }""");
        var schema = new ApiSchema { Type = ApiObjectTypes.Object, DiscriminatorObject = new ApiDiscriminator { PropertyName = "kind" } };
        schema.DiscriminatorObject.Mapping["a"] = new ApiSchema { ReferencePath = "#/components/schemas/A" };
        schema.DiscriminatorObject.Mapping["b"] = new ApiSchema();
        document.Definitions["Base"] = schema;

        var json = ApiDocumentWriter.Write(document);

        json.Should().Contain("\"a\":\"#/components/schemas/A\"").And.Contain("\"b\":null");
    }

    [Test]
    public void Finds_The_First_Path_Of_Objects_That_Appear_Twice()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": { "get": { "responses": { "200": { "description": "ok" } } } }
              }
            }
            """);
        var pathItem = document.Paths["/a"];
        var operation = pathItem["get"];
        var response = operation.Responses["200"];
        var requestBody = new ApiRequestBody();
        var mediaType = new ApiMediaType { Schema = new ApiSchema() };
        requestBody.Content["application/json"] = mediaType;
        requestBody.Content["text/json"] = mediaType;
        operation.RequestBody = requestBody;
        operation.Responses["201"] = response;
        pathItem["post"] = operation;
        document.Paths["/b"] = pathItem;
        document.Components.RequestBodies["Body"] = requestBody;

        var paths = ApiJsonPathFinder.FindReferencePaths(document);

        paths[response].Should().Be("#/paths//a/get/responses/200");
        paths[mediaType].Should().Be("#/paths//a/get/requestBody/content/application/json");
        paths[operation].Should().Be("#/paths//a/get");
        paths[pathItem].Should().Be("#/paths//a");
    }

    [Test]
    public void Operations_And_Parameter_Lists_Implement_Their_Collection_Contracts()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": { "/a": { "get": { "parameters": [{ "name": "a", "in": "query" }], "responses": {} } } }
            }
            """);
        var pathItem = document.Paths["/a"];
        var operation = new ApiOperation();
        pathItem["put"] = operation;
        operation.Parent.Should().BeSameAs(pathItem);
        ((IEnumerable)pathItem).Cast<object>().Should().HaveCount(2);

        var parameters = pathItem["get"].Parameters;
        var first = parameters[0];
        var replacement = new ApiParameter { Name = "b", Kind = ApiParameterKind.Query };
        parameters[0] = replacement;
        parameters.Insert(0, first);
        replacement.Parent.Should().BeSameAs(pathItem["get"]);
        parameters.IsReadOnly.Should().BeFalse();
        parameters.Contains(first).Should().BeTrue();
        parameters.IndexOf(replacement).Should().Be(1);
        ((IEnumerable)parameters).Cast<object>().Should().HaveCount(2);
        parameters.Remove(first).Should().BeTrue();
        parameters.RemoveAt(0);
        parameters.Add(first);
        parameters.Clear();
        parameters.Should().BeEmpty();
    }

    [Test]
    public void Removing_The_Request_Body_Removes_The_Body_Parameter()
    {
        var operation = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": { "/a": { "post": { "requestBody": { "content": { "application/octet-stream": { "schema": { "type": "string", "format": "binary" } } } }, "responses": {} } } }
            }
            """).Paths["/a"]["post"];

        operation.Parameters.Should().ContainSingle(p => p.Kind == ApiParameterKind.Body);
        operation.RequestBody = null;
        operation.UpdateBodyParameter();
        operation.Parameters.Should().BeEmpty();

        operation.Parameters.Add(new ApiParameter { Kind = ApiParameterKind.Body, Schema = new ApiSchema { Type = ApiObjectTypes.String, Format = "binary" } });
        operation.RequestBody!.Content.Keys.Should().Equal("application/octet-stream");
    }

    [Test]
    [Arguments("2024-01-02T03:04:05", DateTimeKind.Unspecified)]
    [Arguments("2024-01-02T03:04:05.1234567Z", DateTimeKind.Utc)]
    [Arguments("2024-01-02T03:04:05+0130", DateTimeKind.Local)]
    [Arguments("2024-01-02T24:00:00Z", DateTimeKind.Utc)]
    [Arguments("0001-01-01T00:00:00+01:00", DateTimeKind.Local)]
    [Arguments("/Date(1700000000000)/", DateTimeKind.Utc)]
    [Arguments("/Date(1700000000000+0100)/", DateTimeKind.Local)]
    public void Parses_Dates_Like_Before(string text, DateTimeKind kind)
    {
        IsoDateTimeParser.TryParse(text, out var value).Should().BeTrue();
        value.Kind.Should().Be(kind);
    }

    [Test]
    public void Midnight_At_The_End_Of_The_Day_Is_The_Next_Day()
    {
        IsoDateTimeParser.TryParse("2024-01-02T24:00:00Z", out var value).Should().BeTrue();

        value.Should().Be(new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    [Arguments("")]
    [Arguments("2024-13-02T03:04:05")]
    [Arguments("2024-02-30T03:04:05")]
    [Arguments("2024-01-02T25:04:05")]
    [Arguments("2024-01-02T24:01:00")]
    [Arguments("2024-01-02T03:60:05")]
    [Arguments("2024-01-02 03:04:05")]
    [Arguments("2024-01-02T03:04:05.12345678")]
    [Arguments("/Date(abc)/")]
    [Arguments("/Date(99999999999999999999)/")]
    public void Does_Not_Parse_Invalid_Dates(string text)
    {
        IsoDateTimeParser.TryParse(text, out _).Should().BeFalse();
    }

    [Test]
    public void Raw_Json_Is_Written_Like_Newtonsoft()
    {
        var obj = new RawJsonObject();
        obj.Properties.Add(new("a", 1L));
        obj.Properties.Add(new("a", 2L));
        obj.Properties.Add(new("empty", new RawJsonArray()));
        obj.Properties.Add(new("text", "tab" + (char)9 + (char)1 + (char)0x2028 + "\"\\" + (char)8 + (char)12 + "\n\r"));
        obj.Properties.Add(new("nan", double.NaN));
        obj.Properties.Add(new("inf", double.PositiveInfinity));
        obj.Properties.Add(new("minf", double.NegativeInfinity));
        obj.Properties.Add(new("guid", Guid.Empty));
        obj.Properties.Add(new("date", new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
        var array = new RawJsonArray();
        array.Items.Add(true);
        array.Items.Add(null);

        obj.TryGetValue("a", out var value).Should().BeTrue();
        value.Should().Be(2L);
        obj.TryGetValue("missing", out _).Should().BeFalse();

        var text = obj.ToString();
        text.Should().Contain("\"empty\": []");
        text.Should().Contain("\\u0001").And.Contain("\\u2028").And.Contain("\\b").And.Contain("\\f");
        text.Should().Contain("NaN").And.Contain("Infinity").And.Contain("-Infinity");
        text.Should().Contain(Guid.Empty.ToString());
        text.Should().Contain("2024-01-02T00:00:00Z");
        array.ToString().Should().Contain("true").And.Contain("null");
        RawJson.ToIndentedString(1.0d).Should().Be("1.0");
        RawJson.ToIndentedString(1.5m).Should().Be(1.5m.ToString(CultureInfo.InvariantCulture));
    }
}
