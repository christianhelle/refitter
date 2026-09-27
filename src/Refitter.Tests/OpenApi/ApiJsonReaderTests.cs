using System.Text.Json;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class ApiJsonReaderTests
{
    private static ApiDocument ReadDocument(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadDocument(document.RootElement);
    }

    private static ApiSchema ReadSchema(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadSchema(document.RootElement);
    }

    private static ApiSchemaProperty ReadProperty(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadSchemaProperty(document.RootElement);
    }

    private static ApiParameter ReadParameter(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadParameter(document.RootElement);
    }

    private static ApiResponse ReadResponse(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadResponse(document.RootElement);
    }

    private static ApiRequestBody ReadRequestBody(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(ApiSchemaType.OpenApi3).ReadRequestBody(document.RootElement);
    }

    private static ApiPathItem ReadPathItem(string json, ApiSchemaType schemaType = ApiSchemaType.OpenApi3)
    {
        using var document = JsonDocument.Parse(json);
        return new ApiJsonReader(schemaType).ReadPathItem(document.RootElement);
    }

    [Test]
    public void Exposes_The_Schema_Type()
    {
        new ApiJsonReader(ApiSchemaType.Swagger2).SchemaType.Should().Be(ApiSchemaType.Swagger2);
    }

    [Test]
    public void Reads_Swagger2_Document_Sections()
    {
        var document = ReadDocument("""
            {
              "swagger": "2.0",
              "x-generator": "tool",
              "host": "example.com",
              "basePath": "/api",
              "schemes": ["https"],
              "externalDocs": { "url": "https://example.com" },
              "info": { "title": "T", "version": "1" },
              "consumes": ["application/json"],
              "produces": ["application/xml"],
              "security": [{ "key": [] }],
              "securityDefinitions": { "key": { "type": "apiKey", "name": "X-Key", "in": "header" } },
              "parameters": { "id": { "name": "id", "in": "path", "type": "string" }, "gone": null },
              "responses": { "error": { "description": "error" } },
              "definitions": { "Pet": { "type": "object" } },
              "components": { "schemas": {} },
              "paths": { "/a": null }
            }
            """, ApiSchemaType.Swagger2);

        document.Swagger.Should().Be("2.0");
        document.Consumes.Should().Equal("application/json");
        document.Produces.Should().Equal("application/xml");
        document.Security.Should().ContainSingle().Which.Should().ContainKey("key");
        document.SecurityDefinitions["key"].In.Should().Be(ApiSecurityApiKeyLocation.Header);
        document.Parameters.Keys.Should().Equal("id");
        document.Responses["error"].Parent.Should().BeSameAs(document);
        document.Definitions.Keys.Should().Equal("Pet");
        document.Paths.Should().BeEmpty();
        document.ExtensionData.Should().ContainKey("components");
        document.ExtensionData.Should().NotContainKeys("x-generator", "host", "basePath", "schemes", "externalDocs");
    }

    [Test]
    public void Reads_OpenApi3_Document_Sections()
    {
        var document = ReadDocument("""
            {
              "openapi": "3.0.1",
              "info": null,
              "servers": [{ "url": "https://example.com" }],
              "consumes": ["application/json"],
              "tags": [{ "name": "pets", "description": "Pets" }, "invalid"],
              "components": {
                "schemas": { "Pet": { "type": "object" }, "Removed": null },
                "requestBodies": { "Body": { "content": {} } },
                "responses": { "Error": { "description": "error" } },
                "parameters": { "Id": { "name": "id", "in": "path" } },
                "headers": { "Rate": { "schema": { "type": "integer" } } },
                "securitySchemes": { "Bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" }, "Oidc": { "type": "openIdConnect", "openIdConnectUrl": "https://example.com" } },
                "links": {}
              }
            }
            """);

        document.OpenApi.Should().Be("3.0.1");
        document.Info.Should().BeNull();
        document.Tags.Should().ContainSingle().Which.Description.Should().Be("Pets");
        document.Components.Schemas.Keys.Should().Equal("Pet");
        document.Components.RequestBodies["Body"].Parent.Should().BeSameAs(document);
        document.Components.Responses["Error"].Parent.Should().BeSameAs(document);
        document.Components.Parameters["Id"].Name.Should().Be("id");
        document.Components.Headers["Rate"].Schema!.Type.Should().Be(ApiObjectType.Integer);
        document.Components.SecuritySchemes["Bearer"].BearerFormat.Should().Be("JWT");
        document.Components.SecuritySchemes["Oidc"].OpenIdConnectUrl.Should().Be("https://example.com");
        document.ExtensionData.Should().ContainKey("consumes");
    }

    [Test]
    public void Ignores_Sections_That_Are_Not_Objects()
    {
        var document = ReadDocument("""
            {
              "swagger": "2.0",
              "info": { "title": "T", "version": "1" },
              "definitions": [],
              "parameters": "none",
              "paths": [],
              "tags": {},
              "security": {}
            }
            """, ApiSchemaType.Swagger2);

        document.Definitions.Should().BeEmpty();
        document.Parameters.Should().BeEmpty();
        document.Paths.Should().BeEmpty();
        document.Tags.Should().BeEmpty();
        document.Security.Should().BeEmpty();
        ReadDocument("""{ "openapi": "3.0.0", "components": [] }""").Components.Schemas.Should().BeEmpty();
    }

    [Test]
    public void Reads_Null_Security_As_Empty()
    {
        ReadDocument("""{ "openapi": "3.0.0", "security": null }""").Security.Should().BeEmpty();
        ReadDocument("""{ "openapi": "3.0.0", "security": ["invalid"] }""").Security.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [Test]
    [Arguments("""{ "version": "1" }""", "title")]
    [Arguments("""{ "title": null, "version": "1" }""", "title")]
    [Arguments("""{ "title": "T" }""", "version")]
    public void Requires_Info_Title_And_Version(string info, string property)
    {
        var act = () => ReadDocument($$"""{ "openapi": "3.0.0", "info": {{info}} }""");

        act.Should().Throw<ApiDocumentReadException>().WithMessage($"*'{property}'*");
    }

    [Test]
    public void Requires_Objects_For_Documents_And_Their_Parts()
    {
        FluentActions.Invoking(() => ReadDocument("[]")).Should().Throw<ApiDocumentReadException>().WithMessage("*document*");
        FluentActions.Invoking(() => ReadSchema("1")).Should().Throw<ApiDocumentReadException>().WithMessage("*schema*");
        FluentActions.Invoking(() => ReadPathItem("true")).Should().Throw<ApiDocumentReadException>().WithMessage("*path item*");
    }

    [Test]
    public void Reads_Path_Items()
    {
        var pathItem = ReadPathItem("""
            {
              "summary": "S",
              "description": "D",
              "servers": [],
              "parameters": [{ "name": "id", "in": "path" }, null],
              "x-Custom": 1,
              "$ref": "#/components/pathItems/A",
              "get": { "responses": {} }
            }
            """);

        pathItem.Summary.Should().Be("S");
        pathItem.Description.Should().Be("D");
        pathItem.Parameters.Should().ContainSingle();
        pathItem.ExtensionData.Should().ContainKey("x-Custom");
        pathItem.ReferencePath.Should().Be("#/components/pathItems/A");
        pathItem.Keys.Should().Equal("get");
        ReadPathItem("""{ "$ref": 1 }""").ReferencePath.Should().Be("1");
    }

    [Test]
    public void Reads_Operations()
    {
        var pathItem = ReadPathItem("""
            {
              "post": {
                "tags": ["pets"],
                "summary": "S",
                "description": "D",
                "operationId": "Op",
                "consumes": ["application/json"],
                "produces": ["application/xml"],
                "parameters": [null, { "name": "q", "in": "query", "type": "string" }],
                "responses": { "200": { "description": "ok" }, "404": null },
                "deprecated": true,
                "security": [{ "key": ["scope"] }],
                "externalDocs": {},
                "schemes": ["https"],
                "x-custom": true
              }
            }
            """, ApiSchemaType.Swagger2);

        var operation = pathItem["post"];
        operation.Tags.Should().Equal("pets");
        operation.Summary.Should().Be("S");
        operation.Description.Should().Be("D");
        operation.OperationId.Should().Be("Op");
        operation.Consumes.Should().Equal("application/json");
        operation.Produces.Should().Equal("application/xml");
        operation.Parameters.Should().ContainSingle();
        operation.Responses.Keys.Should().Equal("200");
        operation.IsDeprecated.Should().BeTrue();
        operation.Security.Should().ContainSingle().Which["key"].Should().Equal("scope");
        operation.ExtensionData.Should().ContainKey("x-custom").And.NotContainKeys("externalDocs", "schemes");
    }

    [Test]
    public void Reads_OpenApi3_Operations()
    {
        var operation = ReadPathItem("""
            {
              "get": {
                "tags": null,
                "requestBody": null,
                "responses": [],
                "callbacks": {},
                "servers": [],
                "consumes": ["ignored"]
              }
            }
            """)["get"];

        operation.Tags.Should().BeEmpty();
        operation.RequestBody.Should().BeNull();
        operation.Responses.Should().BeEmpty();
        operation.Consumes.Should().BeNull();
        operation.ExtensionData.Should().ContainKey("consumes").And.NotContainKeys("callbacks", "servers");
    }

    [Test]
    [Arguments("""{ "get": { "responses": null } }""", "expects a value")]
    [Arguments("""{ "get": { } }""", "not found")]
    public void Requires_Operation_Responses(string json, string message)
    {
        FluentActions.Invoking(() => ReadPathItem(json)).Should().Throw<ApiDocumentReadException>().WithMessage($"*{message}*");
    }

    [Test]
    public void Reads_Request_Bodies()
    {
        var requestBody = ReadRequestBody("""
            {
              "$ref": "#/components/requestBodies/Body",
              "x-name": "payload",
              "description": "D",
              "required": true,
              "x-position": 2,
              "content": { "application/json": { "schema": { "type": "string" }, "example": "text" }, "text/plain": "invalid", "text/csv": { "schema": null } }
            }
            """);

        requestBody.ReferencePath.Should().Be("#/components/requestBodies/Body");
        requestBody.Name.Should().Be("payload");
        requestBody.Description.Should().Be("D");
        requestBody.IsRequired.Should().BeTrue();
        requestBody.Position.Should().Be(2);
        requestBody.Content["application/json"].Example.Should().Be("text");
        requestBody.Content["text/plain"].Schema.Should().BeNull();
        requestBody.Content["text/csv"].Schema.Should().BeNull();
        ReadRequestBody("""{ "content": [] }""").Content.Count.Should().Be(0);
    }

    [Test]
    public void Reads_Swagger2_Responses()
    {
        var response = ReadResponse("""
            {
              "$ref": "#/responses/Error",
              "description": "D",
              "headers": { "X-Rate": { "type": "integer" }, "X-Gone": null },
              "x-nullable": true,
              "schema": { "type": "string" },
              "examples": { "application/json": "text" },
              "x-expectedSchemas": [],
              "content": {}
            }
            """, ApiSchemaType.Swagger2);

        response.ReferencePath.Should().Be("#/responses/Error");
        response.Description.Should().Be("D");
        response.Headers.Keys.Should().Equal("X-Rate");
        response.IsNullableRaw.Should().BeTrue();
        response.Schema!.Type.Should().Be(ApiObjectType.String);
        response.Examples.Should().NotBeNull();
        response.ExtensionData.Should().ContainKey("content").And.NotContainKey("x-expectedSchemas");
        ReadResponse("""{ "schema": null }""", ApiSchemaType.Swagger2).Schema.Should().BeNull();
    }

    [Test]
    public void Reads_OpenApi3_Responses()
    {
        var response = ReadResponse("""
            {
              "headers": [],
              "content": { "application/json": { "schema": { "type": "integer" } } },
              "links": {},
              "x-nullable": true,
              "schema": {}
            }
            """);

        response.Headers.Should().BeEmpty();
        response.Content["application/json"].Schema!.Type.Should().Be(ApiObjectType.Integer);
        response.IsNullableRaw.Should().BeNull();
        response.ExtensionData.Should().ContainKeys("x-nullable", "schema").And.NotContainKey("links");
        ReadResponse("""{ "content": [] }""").Content.Should().BeEmpty();
    }

    [Test]
    public void Reads_Security_Schemes()
    {
        var document = ReadDocument("""
            {
              "swagger": "2.0",
              "securityDefinitions": {
                "basic": { "type": "basic", "description": "D" },
                "oauth": { "type": "OAUTH2", "scheme": "ignored", "bearerFormat": "ignored", "openIdConnectUrl": "ignored" },
                "cookie": { "type": "apiKey", "in": "cookie", "name": "sid" },
                "query": { "type": "apiKey", "in": null }
              }
            }
            """, ApiSchemaType.Swagger2);

        var schemes = document.SecurityDefinitions;
        // Swagger 2.0 basic authentication is HTTP authentication with the basic scheme
        schemes["basic"].Type.Should().Be(ApiSecuritySchemeType.Http);
        schemes["basic"].Scheme.Should().Be("basic");
        schemes["basic"].Description.Should().Be("D");
        schemes["oauth"].Type.Should().Be(ApiSecuritySchemeType.OAuth2);
        schemes["oauth"].Scheme.Should().BeNull();
        schemes["oauth"].BearerFormat.Should().BeNull();
        schemes["oauth"].OpenIdConnectUrl.Should().BeNull();
        schemes["cookie"].In.Should().Be(ApiSecurityApiKeyLocation.Cookie);
        schemes["query"].In.Should().Be(ApiSecurityApiKeyLocation.Undefined);
    }

    [Test]
    public void Requires_Security_Scheme_Type()
    {
        var act = () => ReadDocument("""{ "openapi": "3.0.0", "components": { "securitySchemes": { "a": { "name": "a" } } } }""");

        act.Should().Throw<ApiDocumentReadException>().WithMessage("*'type'*");
    }

    [Test]
    public void Rejects_Unknown_Enum_Values()
    {
        var act = () => ReadParameter("""{ "in": "body-ish" }""");

        act.Should().Throw<ApiDocumentReadException>().WithMessage("*body-ish*ApiParameterKind*");
    }

    [Test]
    public void Reads_Schema_Keywords()
    {
        var schema = ReadSchema("""
            {
              "$schema": "http://json-schema.org/draft-04/schema#",
              "id": "Pet",
              "title": "T",
              "description": "D",
              "format": "f",
              "default": 1,
              "multipleOf": 2,
              "maxLength": 10,
              "minLength": "1",
              "pattern": "^a$",
              "maxItems": 5,
              "minItems": 1,
              "uniqueItems": true,
              "maxProperties": 4,
              "minProperties": 2,
              "deprecated": true,
              "x-deprecatedMessage": "old",
              "x-abstract": true,
              "nullable": true,
              "example": "e",
              "x-enumFlags": true,
              "x-dictionaryKey": { "type": "string" },
              "xml": { "name": "pet" },
              "not": { "type": "null" },
              "patternProperties": { "^x": { "type": "string" } },
              "definitions": { "Inner": { "type": "object" } },
              "x-enumNames": ["A"],
              "x-enum-names": ["B"],
              "x-enum-varnames": ["C"],
              "enum": ["a"],
              "anyOf": [{ "type": "string" }],
              "oneOf": [{ "type": "integer" }],
              "x-custom": 1
            }
            """);

        schema.SchemaVersion.Should().StartWith("http://json-schema.org");
        schema.Id.Should().Be("Pet");
        schema.MultipleOf.Should().Be(2);
        schema.MinLength.Should().Be(1);
        schema.MaxProperties.Should().Be(4);
        schema.MinProperties.Should().Be(2);
        schema.IsDeprecated.Should().BeTrue();
        schema.DeprecatedMessage.Should().Be("old");
        schema.IsAbstract.Should().BeTrue();
        schema.IsNullableRaw.Should().BeTrue();
        schema.Example.Should().Be("e");
        schema.IsFlagEnumerable.Should().BeTrue();
        schema.DictionaryKey!.Type.Should().Be(ApiObjectType.String);
        schema.Not!.Type.Should().Be(ApiObjectType.Null);
        schema.PatternProperties.Keys.Should().Equal("^x");
        schema.Definitions.Keys.Should().Equal("Inner");
        schema.EnumerationNames.Should().Equal("A");
        schema.AnyOf.Should().ContainSingle();
        schema.OneOf.Should().ContainSingle();
        schema.ExtensionData.Should().ContainKey("x-custom").And.NotContainKeys("xml", "x-enum-names", "x-enum-varnames");
    }

    [Test]
    public void Reads_Null_And_Invalid_Schema_Values()
    {
        var schema = ReadSchema("""
            {
              "x-dictionaryKey": null,
              "not": null,
              "items": null,
              "properties": [],
              "patternProperties": [],
              "enum": "a",
              "type": 1,
              "required": null,
              "allOf": {},
              "additionalProperties": null,
              "exclusiveMaximum": null
            }
            """);

        schema.DictionaryKey.Should().BeNull();
        schema.Not.Should().BeNull();
        schema.Item.Should().BeNull();
        schema.Properties.Should().BeEmpty();
        schema.PatternProperties.Should().BeEmpty();
        schema.Enumeration.Should().BeEmpty();
        schema.Type.Should().Be(ApiObjectType.None);
        schema.RequiredProperties.Should().BeEmpty();
        schema.AllOf.Should().BeEmpty();
        schema.AllowAdditionalProperties.Should().BeTrue();
        schema.ExclusiveMaximum.Should().BeNull();
    }

    [Test]
    public void Reads_Keywords_Of_The_Other_Specifications_As_Extension_Data()
    {
        ReadSchema("""{ "x-nullable": true, "x-deprecated": true, "x-example": 1 }""").ExtensionData
            .Should().ContainKeys("x-nullable", "x-deprecated", "x-example");
        ReadSchema("""{ "nullable": true, "deprecated": true }""", ApiSchemaType.Swagger2).ExtensionData
            .Should().ContainKeys("nullable", "deprecated");
        ReadSchema("""{ "example": 1 }""", ApiSchemaType.JsonSchema).ExtensionData.Should().ContainKey("example");

        var swagger2 = ReadSchema("""{ "x-nullable": false, "x-deprecated": true, "example": 1 }""", ApiSchemaType.Swagger2);
        swagger2.IsNullableRaw.Should().BeFalse();
        swagger2.IsDeprecated.Should().BeTrue();
        swagger2.Example.Should().Be(1L);

        ReadSchema("""{ "x-example": 2 }""", ApiSchemaType.JsonSchema).Example.Should().Be(2L);
    }

    [Test]
    public void Reads_Read_And_Write_Only_Per_Specification()
    {
        ReadProperty("""{ "readOnly": true, "writeOnly": true }""").Should().Match<ApiSchemaProperty>(p => p.IsReadOnly && p.IsWriteOnly);
        ReadProperty("""{ "readOnly": true, "x-writeOnly": true }""", ApiSchemaType.Swagger2).Should().Match<ApiSchemaProperty>(p => p.IsReadOnly && p.IsWriteOnly);
        ReadProperty("""{ "readonly": true, "x-writeOnly": true }""", ApiSchemaType.JsonSchema).Should().Match<ApiSchemaProperty>(p => p.IsReadOnly && p.IsWriteOnly);
        ReadProperty("""{ "readOnly": true }""", ApiSchemaType.JsonSchema).ExtensionData.Should().ContainKey("readOnly");
    }

    [Test]
    public void Reads_Additional_Items_And_Properties()
    {
        ReadSchema("""{ "additionalItems": false }""").AllowAdditionalItems.Should().BeFalse();
        ReadSchema("""{ "additionalItems": true }""").AllowAdditionalItems.Should().BeTrue();
        ReadSchema("""{ "additionalItems": "false" }""").AllowAdditionalItems.Should().BeFalse();
        ReadSchema("""{ "additionalItems": { "type": "string" } }""").AdditionalItemsSchema!.Type.Should().Be(ApiObjectType.String);
        ReadSchema("""{ "additionalProperties": "true" }""").AllowAdditionalProperties.Should().BeTrue();
    }

    [Test]
    public void Reads_Tuple_Items()
    {
        var schema = ReadSchema("""{ "items": { "type": "string" } }""");
        schema.Item.Should().NotBeNull();

        var tuple = ReadSchema("""{ "items": [{ "type": "string" }, { "type": "integer" }] }""");
        tuple.Item.Should().BeNull();
        tuple.Items.Should().HaveCount(2);
    }

    [Test]
    public void Reads_Types_Given_As_Lists()
    {
        ReadSchema("""{ "type": ["string", "null"] }""").Type.Should().Be(ApiObjectType.String | ApiObjectType.Null);
        ReadSchema("""{ "type": ["integer", 1] }""").Type.Should().Be(ApiObjectType.Integer);
    }

    [Test]
    public void Reads_Required_Property_Names()
    {
        ReadSchema("""{ "required": ["a", "b"] }""").RequiredProperties.Should().Equal("a", "b");
        FluentActions.Invoking(() => ReadSchema("""{ "required": [1] }""")).Should().Throw<ApiDocumentReadException>();
        FluentActions.Invoking(() => ReadSchema("""{ "required": true }""")).Should().Throw<ApiDocumentReadException>();
    }

    [Test]
    public void Reads_Enum_Descriptions()
    {
        ReadSchema("""{ "x-enumDescriptions": ["a", null] }""").EnumerationDescriptions.Should().Equal("a", null);
        ReadSchema("""{ "x-enum-descriptions": ["b"] }""").EnumerationDescriptions.Should().Equal("b");
        ReadSchema("""{ "x-enumDescriptions": [] }""").EnumerationDescriptions.Should().BeEmpty();
        ReadSchema("""{ "x-enumDescriptions": ["a", 1] }""").EnumerationDescriptions.Should().BeEmpty();
        ReadSchema("""{ "x-enumDescriptions": "a" }""").EnumerationDescriptions.Should().BeEmpty();
    }

    [Test]
    public void Reads_Discriminators()
    {
        ReadSchema("""{ "discriminator": "kind" }""", ApiSchemaType.Swagger2).Discriminator.Should().Be("kind");

        var schema = ReadSchema("""{ "discriminator": { "propertyName": "kind", "mapping": { "dog": "#/components/schemas/Dog" } } }""");
        schema.DiscriminatorObject!.PropertyName.Should().Be("kind");
        schema.DiscriminatorObject.Mapping["dog"].ReferencePath.Should().Be("#/components/schemas/Dog");

        ReadSchema("""{ "discriminator": { "mapping": [] } }""").DiscriminatorObject!.Mapping.Should().BeEmpty();
        ReadSchema("""{ "discriminator": 1 }""").DiscriminatorObject.Should().BeNull();
    }

    [Test]
    public void Reads_Exclusive_Bounds()
    {
        ReadSchema("""{ "exclusiveMaximum": true }""").IsExclusiveMaximum.Should().BeTrue();
        ReadSchema("""{ "exclusiveMaximum": false }""").IsExclusiveMaximum.Should().BeFalse();
        ReadSchema("""{ "exclusiveMinimum": "true" }""").IsExclusiveMinimum.Should().BeTrue();
        ReadSchema("""{ "exclusiveMinimum": 1.50 }""").ExclusiveMinimum.Should().Be(1.5m);
        ReadSchema("""{ "exclusiveMaximum": 1e400 }""").ExclusiveMaximum.Should().Be(decimal.MaxValue);
        ReadSchema("""{ "exclusiveMaximum": "" }""").ExclusiveMaximum.Should().BeNull();
    }

    [Test]
    public void Reads_Numbers_Leniently()
    {
        ReadSchema("""{ "maxLength": 1.0 }""").MaxLength.Should().Be(1);
        ReadSchema("""{ "maxLength": "" }""").MaxLength.Should().BeNull();
        ReadSchema("""{ "maxLength": null }""").MaxLength.Should().BeNull();
        ReadSchema("""{ "maxItems": null }""").MaxItems.Should().Be(0);
        ReadSchema("""{ "maximum": "5" }""").Maximum.Should().Be(5);
        ReadSchema("""{ "maximum": "" }""").Maximum.Should().BeNull();
        ReadSchema("""{ "maximum": null }""").Maximum.Should().BeNull();

        FluentActions.Invoking(() => ReadSchema("""{ "maxLength": [] }""")).Should().Throw<ApiDocumentReadException>();
        FluentActions.Invoking(() => ReadSchema("""{ "maximum": {} }""")).Should().Throw<ApiDocumentReadException>();
        FluentActions.Invoking(() => ReadSchema("""{ "multipleOf": 1e400 }""")).Should().Throw<ApiDocumentReadException>();
        FluentActions.Invoking(() => ReadSchema("""{ "maximum": "abc" }""")).Should().Throw<ApiDocumentReadException>();
    }

    [Test]
    public void Reads_Booleans_Leniently()
    {
        ReadSchema("""{ "uniqueItems": "True" }""").UniqueItems.Should().BeTrue();
        ReadSchema("""{ "uniqueItems": 1 }""").UniqueItems.Should().BeTrue();
        ReadSchema("""{ "uniqueItems": 0 }""").UniqueItems.Should().BeFalse();
        ReadSchema("""{ "uniqueItems": null }""").UniqueItems.Should().BeFalse();
        FluentActions.Invoking(() => ReadSchema("""{ "uniqueItems": "yes" }""")).Should().Throw<ApiDocumentReadException>();
    }

    [Test]
    public void Reads_Strings_Leniently()
    {
        ReadSchema("""{ "title": true }""").Title.Should().Be("true");
        ReadSchema("""{ "title": false }""").Title.Should().Be("false");
        ReadSchema("""{ "title": 1.5 }""").Title.Should().Be("1.5");
        FluentActions.Invoking(() => ReadSchema("""{ "title": {} }""")).Should().Throw<ApiDocumentReadException>();
        FluentActions.Invoking(() => ReadSchema("""{ "x-enumNames": "a" }""")).Should().Throw<ApiDocumentReadException>();
        ReadSchema("""{ "x-enumNames": null }""").EnumerationNames.Should().BeEmpty();
    }

    [Test]
    public void Reads_Parameter_Keywords()
    {
        var parameter = ReadParameter("""
            {
              "name": "ids",
              "x-originalName": "Ids",
              "in": "Query",
              "style": "form",
              "explode": false,
              "required": true,
              "allowEmptyValue": true,
              "description": "D",
              "collectionFormat": "multi",
              "examples": {},
              "schema": { "type": "array" },
              "x-schema": { "type": "string" },
              "x-position": 3,
              "title": "T"
            }
            """);

        parameter.Name.Should().Be("ids");
        parameter.OriginalName.Should().Be("Ids");
        parameter.Kind.Should().Be(ApiParameterKind.Query);
        parameter.Style.Should().Be(ApiParameterStyle.Form);
        parameter.Explode.Should().BeFalse();
        parameter.IsRequired.Should().BeTrue();
        parameter.AllowEmptyValue.Should().BeTrue();
        parameter.Description.Should().Be("D");
        parameter.CollectionFormat.Should().Be(ApiParameterCollectionFormat.Multi);
        parameter.Schema!.Type.Should().Be(ApiObjectType.Array);
        parameter.CustomSchema!.Type.Should().Be(ApiObjectType.String);
        parameter.Position.Should().Be(3);
        parameter.Title.Should().Be("T");
        parameter.ExtensionData.Should().BeNull();
    }

    [Test]
    public void Reads_Swagger2_Parameter_Keywords_As_Extension_Data()
    {
        var parameter = ReadParameter("""
            { "name": null, "in": null, "examples": {}, "x-position": 1, "title": "T", "schema": null, "x-schema": null }
            """, ApiSchemaType.Swagger2);

        parameter.Name.Should().BeEmpty();
        parameter.Kind.Should().Be(ApiParameterKind.Undefined);
        parameter.Position.Should().BeNull();
        parameter.Title.Should().BeNull();
        parameter.Schema.Should().BeNull();
        parameter.CustomSchema.Should().BeNull();
        parameter.ExtensionData.Should().ContainKeys("examples", "x-position", "title");
    }
}
