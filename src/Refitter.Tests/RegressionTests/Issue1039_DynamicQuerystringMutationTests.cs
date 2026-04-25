using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

/// <summary>
/// Regression tests for Issue #1039: dynamic querystring extraction mutates the shared NSwag model.
/// Validates that XML documentation still sees the original query parameters after wrapper generation.
/// </summary>
public class Issue1039_DynamicQuerystringMutationTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Search API",
            "version": "v1"
          },
          "paths": {
            "/search": {
              "get": {
                "operationId": "SearchItems",
                "summary": "Search items",
                "tags": [
                  "search"
                ],
                "parameters": [
                  {
                    "name": "query",
                    "in": "query",
                    "description": "Search text",
                    "required": true,
                    "schema": {
                      "type": "string"
                    }
                  },
                  {
                    "name": "page",
                    "in": "query",
                    "description": "Page number",
                    "schema": {
                      "type": "integer"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    [Arguments(MultipleInterfaces.Unset)]
    [Arguments(MultipleInterfaces.ByTag)]
    [Arguments(MultipleInterfaces.ByEndpoint)]
    public async Task Dynamic_Querystring_Generation_Preserves_Original_Query_Param_Documentation(
        MultipleInterfaces multipleInterfaces)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(OpenApiSpec, "issue-1039.json");
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseDynamicQuerystringParameters = true,
            GenerateXmlDocCodeComments = true,
            MultipleInterfaces = multipleInterfaces
        };

        var sut = await RefitGenerator.CreateAsync(settings);

        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("/// <param name=\"query\">Search text</param>");
        generatedCode.Should().Contain("/// <param name=\"page\">Page number</param>");
        generatedCode.Should().Contain("/// <param name=\"queryParams\">The dynamic querystring parameter wrapping all others.</param>");
        generatedCode.Should().Contain("QueryParams queryParams");
    }
}
