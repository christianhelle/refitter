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
    public async Task Uses_Event_Data_As_Item_Type_For_Server_Sent_Events(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<MyStreamData> MyStreamEndpoint(");
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

    // Each JSON line is a whole item, so the item schema itself is the stream's item type
    private static readonly string JsonLinesSpec = OpenApi32Spec.Replace("text/event-stream", "application/jsonl");

    [Test]
    public async Task Uses_ItemSchema_As_Item_Type_For_Json_Lines()
    {
        string generatedCode = await GenerateCode(JsonLinesSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous> MyStreamEndpoint(");
        generatedCode.Should().Contain("public MyStreamData Data { get; set; }");
    }

    // Without a data field the event shape is unknown, so the item schema itself is the item type
    private static readonly string EventsWithoutDataSpec = OpenApi32Spec.Replace("\"data\"", "\"payload\"");

    [Test]
    public async Task Uses_ItemSchema_As_Item_Type_For_Server_Sent_Events_Without_Data()
    {
        string generatedCode = await GenerateCode(EventsWithoutDataSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous> MyStreamEndpoint(");
        generatedCode.Should().Contain("public MyStreamData Payload { get; set; }");
    }

    // ASP.NET Core 11 can reference an event data schema it never adds to the components
    private static readonly string UnresolvedReferenceSpec = OpenApi32Spec.Replace(
        "#/components/schemas/MyStreamData",
        "#/components/schemas/Missing");

    [Test]
    public async Task Ignores_ItemSchema_With_Unresolved_Reference()
    {
        string generatedCode = await GenerateCode(UnresolvedReferenceSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<object> MyStreamEndpoint(");
    }

    private static readonly string ParameterizedEventStreamSpec = OpenApi32Spec.Replace(
        "\"text/event-stream\"",
        "\"text/event-stream; charset=utf-8\"");

    [Test]
    public async Task Uses_Event_Data_As_Item_Type_For_Parameterized_Event_Stream()
    {
        string generatedCode = await GenerateCode(ParameterizedEventStreamSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<MyStreamData> MyStreamEndpoint(");
    }

    // A reference can point into an array, like the first entry of an allOf
    private static readonly string ArrayPointerSpec = OpenApi32Spec
        .Replace(
            "\"$ref\": \"#/components/schemas/MyStreamData\"",
            "\"$ref\": \"#/components/schemas/Payload/allOf/0\"")
        .Replace(
            "\"schemas\": {",
            """
            "schemas": {
                          "Payload": { "allOf": [ { "$ref": "#/components/schemas/MyStreamData" } ] },
            """);

    [Test]
    public async Task Resolves_Event_Data_Reference_Into_An_Array()
    {
        // NSwag names the item type after the component that holds the array
        string generatedCode = await GenerateCode(ArrayPointerSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<Payload> MyStreamEndpoint(");
    }

    private static readonly string ArrayPointerOutOfRangeSpec = ArrayPointerSpec.Replace(
        "#/components/schemas/Payload/allOf/0",
        "#/components/schemas/Payload/allOf/1");

    [Test]
    public async Task Ignores_ItemSchema_With_Reference_Past_The_End_Of_An_Array()
    {
        string generatedCode = await GenerateCode(ArrayPointerOutOfRangeSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<object> MyStreamEndpoint(");
    }

    // A boolean schema allows any item, so there is no item type to generate
    private const string BooleanItemSchemaSpec = """
        {
          "openapi": "3.2.0",
          "info": { "title": "Boolean item schema", "version": "1.0.0" },
          "paths": {
            "/": {
              "get": {
                "operationId": "MyStreamEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "text/event-stream": { "itemSchema": true }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Uses_Object_For_Boolean_ItemSchema()
    {
        string generatedCode = await GenerateCode(BooleanItemSchemaSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<object> MyStreamEndpoint(");
    }

    // The event envelope can be a component of its own, possibly behind another reference
    private const string ReferencedEnvelopeSpec = """
        {
          "openapi": "3.2.0",
          "info": { "title": "Referenced envelope", "version": "1.0.0" },
          "paths": {
            "/": {
              "get": {
                "operationId": "MyStreamEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "text/event-stream": {
                        "itemSchema": { "$ref": "#/components/schemas/EventAlias" }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "EventAlias": { "$ref": "#/components/schemas/Event" },
              "Event": {
                "type": "object",
                "properties": {
                  "data": { "$ref": "#/components/schemas/MyStreamData" },
                  "event": { "type": "string" }
                }
              },
              "MyStreamData": {
                "type": "object",
                "properties": {
                  "message": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Uses_Event_Data_As_Item_Type_For_Referenced_Event_Envelope()
    {
        string generatedCode = await GenerateCode(ReferencedEnvelopeSpec);
        generatedCode.Should().Contain("IAsyncEnumerable<MyStreamData> MyStreamEndpoint(");
    }

    // A reference cycle has no envelope to unwrap. Unwrapping must stop, leaving NSwag to report the cycle
    private static readonly string ReferenceCycleSpec = ReferencedEnvelopeSpec.Replace(
        "\"Event\": {",
        "\"Event\": { \"$ref\": \"#/components/schemas/EventAlias\" },\n\"Unused\": {");

    [Test]
    public async Task Stops_Unwrapping_Referenced_Event_Envelope_Cycle()
    {
        Func<Task> act = () => GenerateCode(ReferenceCycleSpec);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cyclic references*");
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

    private const string OpenApi32YamlSpec = """
        openapi: 3.2.0
        info:
          title: Program-SSE | v1
          version: 1.0.0
        paths:
          /:
            get:
              operationId: MyStreamEndpoint
              responses:
                '200':
                  description: Success
                  content:
                    text/event-stream:
                      itemSchema:
                        required:
                          - data
                        type: object
                        properties:
                          data:
                            $ref: '#/components/schemas/MyStreamData'
                          event:
                            type: string
                          id:
                            type: string
        components:
          schemas:
            MyStreamData:
              type: object
              properties:
                message:
                  type: string
                index:
                  type: integer
                  format: int32
        """;

    [Test]
    public async Task Uses_Event_Data_As_Item_Type_For_Yaml_Server_Sent_Events()
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApi32YamlSpec);
        string generatedCode = await GenerateCodeFromFile(swaggerFile);
        generatedCode.Should().Contain("IAsyncEnumerable<MyStreamData> MyStreamEndpoint(");
    }

    // A property named "content" is a schema, not a content map, so its "itemSchema" property is left alone
    private static readonly string ContentPropertySpec = OpenApi32Spec.Replace(
        "\"message\": { \"type\": \"string\" },",
        """
        "message": { "type": "string" },
                          "content": {
                            "type": "object",
                            "properties": {
                              "itemSchema": { "type": "string" }
                            }
                          },
        """);

    [Test]
    public async Task Leaves_Schema_Properties_Named_ItemSchema_Untouched()
    {
        string generatedCode = await GenerateCode(ContentPropertySpec);
        generatedCode.Should().Contain("public string ItemSchema { get; set; }");
    }

    private static async Task<string> GenerateCode(string spec)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        return await GenerateCodeFromFile(swaggerFile);
    }

    private static async Task<string> GenerateCodeFromFile(string swaggerFile)
    {
        RefitGeneratorSettings settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
