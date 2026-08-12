using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;


public class IAsyncEnumerableResponseTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Streaming API
  version: 1.0.0
paths:
  '/events':
    get:
      operationId: getEvents
      summary: Stream a list of events as JSON lines
      responses:
        '200':
          description: A stream of events
          content:
            application/x-ndjson:
              schema:
                type: array
                items:
                  type: object
                  properties:
                    id:
                      type: integer
                      format: int64
                    name:
                      type: string
  '/events/jsonl':
    get:
      operationId: getEventsJsonl
      summary: Stream events using application/jsonl
      responses:
        '200':
          description: A stream of events
          content:
            application/jsonl:
              schema:
                type: array
                items:
                  type: object
                  properties:
                    id:
                      type: integer
                      format: int64
                    name:
                      type: string
  '/events/sse':
    get:
      operationId: getEventsSse
      summary: Stream events using text/event-stream
      responses:
        '200':
          description: A stream of events
          content:
            text/event-stream:
              schema:
                type: array
                items:
                  type: object
                  properties:
                    id:
                      type: integer
                      format: int64
                    name:
                      type: string
  '/events/untyped':
    get:
      operationId: getEventsUntyped
      summary: Stream events without a schema
      responses:
        '200':
          description: A stream of untyped events
          content:
            text/event-stream: {}
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_For_NDJson()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<");
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_For_NDJson_Method()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous> GetEvents(");
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_For_Jsonl_Method()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous2> GetEventsJsonl(");
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_For_EventStream_Method()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous3> GetEventsSse(");
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_Of_Object_For_Untyped_Streaming_Response()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<object> GetEventsUntyped(");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        string generatedCode = sut.Generate();
        return generatedCode;
    }
}
