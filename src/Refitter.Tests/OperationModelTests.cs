using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class OperationModelTests
{
    private static (OperationModel Model, ApiOperation Operation) Create(string operationJson, string header = "\"openapi\": \"3.0.1\"", string extra = "")
    {
        var document = ApiDocumentLoader.Load(
            $$"""{ {{header}}, "info": { "title": "T", "version": "1" }, "paths": { "/a": { "post": {{operationJson}} } } {{extra}} }""",
            null,
            isYaml: false);
        var generator = new ContractGeneratorFactory(new RefitGeneratorSettings(), document).Create();
        var operation = document.Paths["/a"]["post"];
        return (generator.CreateOperationModel(operation), operation);
    }

    [Test]
    public void Exposes_The_Operation_And_Its_Names()
    {
        var (model, operation) = Create("""{ "operationId": "Op", "responses": { "200": { "description": "ok" } } }""");

        model.Operation.Should().BeSameAs(operation);
        model.ActualOperationName.Should().BeNull();
        model.OperationName = "get_pets";
        model.ActualOperationName.Should().Be("Get_pets");
    }

    [Test]
    public void Rejects_Multiple_Body_Parameters()
    {
        var (model, _) = Create("""
            {
              "operationId": "Op",
              "parameters": [{ "name": "a", "in": "body", "schema": {} }, { "name": "b", "in": "body", "schema": {} }],
              "responses": { "200": { "description": "ok" } }
            }
            """, "\"swagger\": \"2.0\"");

        FluentActions.Invoking(() => model.ContentParameter).Should().Throw<InvalidOperationException>().WithMessage("*Multiple body parameters*");
    }

    [Test]
    public void Describes_Consumed_Content()
    {
        var (json, _) = Create("""{ "consumes": ["application/json"], "responses": {} }""", "\"swagger\": \"2.0\"");
        json.ConsumesJson.Should().BeTrue();

        var (form, _) = Create("""{ "requestBody": { "content": { "application/x-www-form-urlencoded": {} } }, "responses": {} }""");
        form.ConsumesOnlyFormUrlEncoded.Should().BeTrue();
    }

    [Test]
    public void Describes_Results()
    {
        var (model, _) = Create("""
            {
              "responses": {
                "200": { "description": "ok", "content": { "application/json": { "schema": { "title": "Named", "type": "object", "properties": { "a": { "type": "string" } } } } } }
              }
            }
            """);

        model.UnwrappedResultType.Should().Be("Named");
        model.UnwrappedResultDefaultValue.Should().Be("default(Named)");
        model.ResultDescription.Should().Be("ok");

        var (empty, _) = Create("""{ "responses": { "204": { "description": "none" } } }""");
        empty.UnwrappedResultDefaultValue.Should().BeNull();
    }

    [Test]
    public void Describes_Exceptions_That_Derive_From_The_Exception_Schema()
    {
        var (model, _) = Create(
            """
            {
              "responses": {
                "200": { "description": "ok" },
                "400": { "description": "", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Problem" } } } },
                "500": { "description": "failed", "content": { "application/json": { "schema": { "type": "string" } } } }
              }
            }
            """,
            extra: """
            , "components": {
                "schemas": {
                  "Exception": { "type": "object", "properties": { "message": { "type": "string" } } },
                  "Problem": { "allOf": [{ "$ref": "#/components/schemas/Exception" }, { "type": "object" }] }
                }
              }
            """);

        var exception = model.ExceptionDescriptions.Should().ContainSingle().Subject;
        exception.Type.Should().Be("ApiException{Problem}");
        exception.Description.Should().Be("A server side error occurred.");
    }

    [Test]
    public void Resolves_Parameter_Types()
    {
        var (binary, _) = Create("""
            { "requestBody": { "content": { "application/octet-stream": { "schema": { "type": "string", "format": "binary" } }, "image/png": { "schema": { "type": "string", "format": "binary" } } } }, "responses": {} }
            """);
        binary.Parameters.Single().Type.Should().Be("FileParameter");

        var (xml, _) = Create("""
            { "requestBody": { "content": { "application/xml": { "schema": { "type": "object" } } } }, "responses": {} }
            """);
        xml.Parameters.Single().Type.Should().Be("string");

        var (swagger, _) = Create("""
            {
              "parameters": [
                { "name": "ids", "in": "query", "type": "integer", "collectionFormat": "multi" },
                { "name": "files", "in": "formData", "type": "file", "collectionFormat": "multi" }
              ],
              "responses": {}
            }
            """, "\"swagger\": \"2.0\"");
        swagger.Parameters.Single(p => p.Name == "ids").Type.Should().Be("System.Collections.Generic.IEnumerable<int?>");
        swagger.Parameters.Single(p => p.Name == "files").Type.Should().Be("System.Collections.Generic.IEnumerable<FileParameter>");
    }

    [Test]
    public void Describes_Array_Parameters()
    {
        var (model, _) = Create("""
            {
              "parameters": [
                { "name": "tags", "in": "query", "explode": false, "schema": { "type": "array", "items": { "type": "string" } } },
                { "name": "days", "in": "query", "schema": { "type": "array", "items": { "type": "string", "format": "date" } } }
              ],
              "responses": {}
            }
            """);

        var tags = model.Parameters.Single(p => p.Name == "tags");
        tags.Parameter.Name.Should().Be("tags");
        tags.IsExplodedArray.Should().BeFalse();
        tags.IsStringArray.Should().BeTrue();
        model.Parameters.Single(p => p.Name == "days").IsDateArray.Should().BeTrue();
    }

    [Test]
    public void Explodes_OpenApi3_Query_And_Cookie_Arrays_Unless_Told_Otherwise()
    {
        var (model, _) = Create("""
            {
              "parameters": [
                { "name": "q", "in": "query", "schema": { "type": "array", "items": { "type": "string" } } },
                { "name": "c", "in": "cookie", "schema": { "type": "array", "items": { "type": "string" } } },
                { "name": "h", "in": "header", "schema": { "type": "array", "items": { "type": "string" } } },
                { "name": "e", "in": "header", "explode": true, "schema": { "type": "array", "items": { "type": "string" } } },
                { "name": "s", "in": "query", "schema": { "type": "string" } }
              ],
              "responses": {}
            }
            """);

        model.Parameters.ToDictionary(p => p.Name, p => p.IsExplodedArray)
            .Should().Equal(new Dictionary<string, bool> { ["q"] = true, ["c"] = true, ["h"] = false, ["e"] = true, ["s"] = false });
    }

    [Test]
    public void Explodes_Swagger2_Arrays_Only_With_The_Multi_Collection_Format()
    {
        var (model, _) = Create("""
            {
              "parameters": [
                { "name": "csv", "in": "query", "type": "array", "items": { "type": "string" } },
                { "name": "multi", "in": "query", "type": "array", "collectionFormat": "multi", "items": { "type": "string" } }
              ],
              "responses": {}
            }
            """, "\"swagger\": \"2.0\"");

        model.Parameters.ToDictionary(p => p.Name, p => p.IsExplodedArray)
            .Should().Equal(new Dictionary<string, bool> { ["csv"] = false, ["multi"] = true });
    }

    [Test]
    public void Describes_Responses()
    {
        var (model, _) = Create("""
            {
              "responses": {
                "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "string", "format": "date-time" } } } },
                "201": { "description": "ok", "content": { "application/json": { "schema": { "type": "integer" } } } },
                "202": { "description": "ok", "content": { "application/pdf": { "schema": { "type": "string", "format": "binary" } } } }
              }
            }
            """);

        var responses = model.Responses.ToDictionary(r => r.StatusCode);
        responses["200"].IsDate.Should().BeTrue();
        responses["201"].IsSuccess.Should().BeFalse();
        responses["202"].IsFile.Should().BeFalse();
    }
}
