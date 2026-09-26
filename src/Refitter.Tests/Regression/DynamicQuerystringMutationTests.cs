using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Regression;

/// <summary>
/// Regression tests for Issue #1039: dynamic querystring extraction mutates the shared NSwag model.
/// Validates that XML documentation still sees the original query parameters after wrapper generation.
/// </summary>

public class DynamicQuerystringMutationTests
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

        // The query parameters are folded into the wrapper, so their descriptions document its properties
        // and the method only documents the parameters it has (#1263)
        generatedCode.Should().Contain("/// Search text");
        generatedCode.Should().Contain("/// Page number");
        generatedCode.Should().NotContain("<param name=\"query\">");
        generatedCode.Should().NotContain("<param name=\"page\">");
        generatedCode.Should().Contain("/// <param name=\"queryParams\">The dynamic querystring parameter wrapping all others.</param>");
        generatedCode.Should().Contain("QueryParams queryParams");
    }
}
