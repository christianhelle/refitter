using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// x-enumDescriptions as an object keyed by enum value, as docusaurus-theme-openapi-docs renders it (#1336).
/// </summary>
public class ObjectEnumDescriptionsTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.1.1",
          "info": { "title": "Repro", "version": "v1" },
          "paths": {
            "/status": {
              "get": {
                "operationId": "GetStatus",
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": { "schema": { "$ref": "#/components/schemas/Status" } }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Status": {
                "type": "string",
                "enum": ["pending", "done"],
                "x-enumDescriptions": { "pending": "Not started yet.", "done": "Finished." }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return sut.Generate();
    }
}
