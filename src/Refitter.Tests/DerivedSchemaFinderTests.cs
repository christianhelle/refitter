using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class DerivedSchemaFinderTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    // A schema that derives from Base, with a unique title so that the tests can tell them apart
    private static string Derived(string name) =>
        $$"""{ "title": "{{name}}", "allOf": [{ "$ref": "#/components/schemas/Base" }, { "type": "object" }] }""";

    private static string SwaggerDerived(string name) =>
        $$"""{ "title": "{{name}}", "allOf": [{ "$ref": "#/definitions/Base" }, { "type": "object" }] }""";

    [Test]
    public void Finds_Derived_Schemas_Everywhere_In_OpenApi3_Documents()
    {
        var document = Load($$"""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "parameters": [{ "name": "p", "in": "query", "schema": {{Derived("PathParameter")}} }],
                  "post": {
                    "requestBody": { "$ref": "#/components/requestBodies/Body" },
                    "responses": { "200": { "$ref": "#/components/responses/Ok" } }
                  }
                },
                "/b": { "$ref": "#/paths/~1a" }
              },
              "components": {
                "schemas": {
                  "Base": { "type": "object", "discriminator": { "propertyName": "kind" } },
                  "Holder": {
                    "type": "object",
                    "additionalItems": {{Derived("AdditionalItems")}},
                    "items": [{{Derived("TupleItem")}}],
                    "anyOf": [{{Derived("AnyOf")}}],
                    "oneOf": [{{Derived("OneOf")}}],
                    "not": {{Derived("Not")}},
                    "x-dictionaryKey": {{Derived("DictionaryKey")}},
                    "patternProperties": { "^x": {{Derived("PatternProperty")}} },
                    "definitions": { "Nested": {{Derived("Definition")}} }
                  }
                },
                "requestBodies": { "Body": { "content": { "application/json": { "schema": {{Derived("RequestBody")}} } } } },
                "responses": { "Ok": { "description": "ok", "content": { "application/json": { "schema": {{Derived("Response")}} } } } },
                "parameters": { "Id": { "name": "id", "in": "query", "schema": {{Derived("Parameter")}}, "x-schema": {{Derived("CustomSchema")}} } },
                "headers": { "Rate": { "schema": {{Derived("Header")}} } }
              }
            }
            """);

        var found = DerivedSchemaFinder.Find(document.Definitions["Base"], document).Select(p => p.Key.Title).ToList();

        found.Should().BeEquivalentTo(
            "PathParameter", "RequestBody", "Response", "AdditionalItems", "TupleItem", "AnyOf", "OneOf", "Not",
            "DictionaryKey", "PatternProperty", "Definition", "Parameter", "CustomSchema", "Header");
    }

    [Test]
    public void Finds_Derived_Schemas_Everywhere_In_Swagger2_Documents()
    {
        var document = Load($$"""
            {
              "swagger": "2.0",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/a": {
                  "get": {
                    "parameters": [{ "name": "body", "in": "body", "schema": {{SwaggerDerived("BodyParameter")}} }],
                    "responses": { "200": { "$ref": "#/responses/Ok" } }
                  }
                }
              },
              "definitions": { "Base": { "type": "object", "discriminator": "kind" } },
              "parameters": { "Id": { "name": "id", "in": "body", "schema": {{SwaggerDerived("Parameter")}} } },
              "responses": { "Ok": { "description": "ok", "schema": {{SwaggerDerived("Response")}}, "headers": { "X": { "type": "string" } } } }
            }
            """);

        var found = DerivedSchemaFinder.Find(document.Definitions["Base"], document).ToList();

        found.Select(p => p.Key.Title).Should().BeEquivalentTo("BodyParameter", "Parameter", "Response");
        found.Single(p => p.Key.Title == "Response").Value.Should().Be("schema");
    }
}
