using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.2 QUERY operations. Refit has no QUERY verb attribute and Refit.QueryAttribute is the
/// parameter attribute, so the client gets its own HttpMethodAttribute subclass.
/// </summary>
[TestClass]
public class QueryOperationTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.2.0",
          "info": { "title": "Items", "version": "v1" },
          "paths": {
            "/items": {
              "get": {
                "operationId": "GetItems",
                "parameters": [ { "name": "q", "in": "query", "required": true, "schema": { "type": "string" } } ],
                "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "array", "items": { "type": "string" } } } } } }
              },
              "query": {
                "operationId": "SearchItems",
                "requestBody": {
                  "required": true,
                  "content": { "application/json": { "schema": { "$ref": "#/components/schemas/SearchBody" } } }
                },
                "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "array", "items": { "type": "string" } } } } } }
              }
            }
          },
          "components": {
            "schemas": {
              "SearchBody": { "type": "object", "properties": { "q": { "type": "string" } } }
            }
          }
        }
        """;

    private static readonly string WithoutQuerySpec = OpenApiSpec
        .Replace("\"query\": {", "\"post\": {");

    [TestMethod]
    public async Task Generates_QueryMethod_Attribute_On_Query_Operation()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[QueryMethod(\"/items\")]");
        generatedCode.Should().NotContain("[Query(\"/items\")]");
    }

    [TestMethod]
    public async Task Keeps_Query_Attribute_On_Query_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] string q");
    }

    [TestMethod]
    public async Task Generates_QueryMethodAttribute_Class()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("sealed class QueryMethodAttribute : global::Refit.HttpMethodAttribute");
        // The Refit source generator only inlines a verb constructed from a string literal
        generatedCode.Should().Contain("Method => new global::System.Net.Http.HttpMethod(\"QUERY\");");
    }

    [TestMethod]
    public async Task Does_Not_Generate_QueryMethodAttribute_Class_Without_Query_Operations()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(WithoutQuerySpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        sut.Generate().Should().NotContain("QueryMethod");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code_Without_Refit_Warnings()
    {
        // Warnings as errors also fails on RF006, which the Refit source generator reports
        // when it cannot build the request without the reflection fallback
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(warningsAsErrors: true, generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(MultipleInterfaces.ByEndpoint)]
    [DataRow(MultipleInterfaces.ByTag)]
    public async Task Can_Build_Generated_Multiple_Files(MultipleInterfaces multipleInterfaces)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(
            new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                MultipleInterfaces = multipleInterfaces,
                GenerateMultipleFiles = true,
            });

        var files = sut.GenerateMultipleFiles().Files;
        files.Should().ContainSingle(f => f.Filename == "QueryMethodAttribute.cs");
        BuildHelper.BuildCSharp(warningsAsErrors: true, files.Select(f => f.Content).ToArray()).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return sut.Generate();
    }
}
