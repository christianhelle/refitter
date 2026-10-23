using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class ItemSchemaStreamingResponseTests
{
    private const string OpenApi32Spec = """
        {
          "openapi": "3.2.0",
          "info": { "title": "Program-SSE | v1", "version": "1.0.0" },
          "paths": {
            "/": {
              "get": {
                "operationId": "MyStreamEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "text/event-stream": {
                        "itemSchema": {
                          "required": [ "data" ],
                          "type": "object",
                          "properties": {
                            "data": { "$ref": "#/components/schemas/MyStreamData" },
                            "event": { "type": "string" },
                            "id": { "type": "string" }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "MyStreamData": {
                "type": "object",
                "properties": {
                  "message": { "type": "string" },
                  "index": { "type": "integer", "format": "int32" }
                }
              }
            }
          }
        }
        """;

    // OpenAPI 3.1 has no itemSchema, so Microsoft.OpenApi writes it as an extension
    private static readonly string OpenApi31Spec = OpenApi32Spec
        .Replace("\"3.2.0\"", "\"3.1.0\"")
        .Replace("\"itemSchema\"", "\"x-oai-itemSchema\"");

    public static IEnumerable<string> Specs()
    {
        yield return OpenApi32Spec;
        yield return OpenApi31Spec;
    }

    [Test]
    [MethodDataSource(nameof(Specs))]
    public async Task Generates_Typed_IAsyncEnumerable_From_ItemSchema(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous> MyStreamEndpoint(");
        generatedCode.Should().Contain("public MyStreamData Data { get; set; }");
    }

    [Test]
    [Category("Integration")]
    [MethodDataSource(nameof(Specs))]
    public async Task Can_Build_Generated_Code_From_ItemSchema(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static readonly string SchemaAndItemSchemaSpec = OpenApi32Spec.Replace(
        "\"itemSchema\": {",
        """
        "schema": { "type": "array", "items": { "$ref": "#/components/schemas/MyStreamData" } },
                                "itemSchema": {
        """);

    [Test]
    public async Task Prefers_Schema_Over_ItemSchema()
    {
        string generatedCode = await GenerateCode(SchemaAndItemSchemaSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<MyStreamData> MyStreamEndpoint(");
    }

    private static async Task<string> GenerateCode(string spec)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
