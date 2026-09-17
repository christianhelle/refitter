using AwesomeAssertions;
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

    private const string Swagger2Spec = @"
swagger: '2.0'
info:
  title: Streaming API
  version: 1.0.0
paths:
  '/events':
    get:
      operationId: getEvents
      summary: Stream a list of events as JSON lines
      produces:
        - application/x-ndjson
      responses:
        '200':
          description: A stream of events
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
      produces:
        - application/jsonl
      responses:
        '200':
          description: A stream of events
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
      produces:
        - text/event-stream
      responses:
        '200':
          description: A stream of events
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
      produces:
        - text/event-stream
      responses:
        '200':
          description: A stream of untyped events
";

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Can_Generate_Code(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Generates_IAsyncEnumerable_For_NDJson(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Generates_IAsyncEnumerable_For_NDJson_Method(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous> GetEvents(");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Generates_IAsyncEnumerable_For_Jsonl_Method(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous2> GetEventsJsonl(");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Generates_IAsyncEnumerable_For_EventStream_Method(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<Anonymous3> GetEventsSse(");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Generates_IAsyncEnumerable_Of_Object_For_Untyped_Streaming_Response(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        generatedCode.Should().Contain("IAsyncEnumerable<object> GetEventsUntyped(");
    }

    [Test]
    [Category("Integration")]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Can_Build_Generated_Code(string spec)
    {
        string generatedCode = await GenerateCode(spec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private const string MixedJsonAndPrimitiveNdjsonSpec = @"
openapi: '3.0.0'
info:
  title: Task Instance Logs
  version: 1.0.0
paths:
  '/logs':
    get:
      operationId: getLog
      summary: Get Log
      responses:
        '200':
          description: Successful Response
          content:
            application/json:
              schema:
                type: object
                properties:
                  content:
                    type: string
            application/x-ndjson:
              schema:
                type: string
";

    [Test]
    public async Task Does_Not_Generate_IAsyncEnumerable_When_Ndjson_Schema_Is_String()
    {
        string generatedCode = await GenerateCode(MixedJsonAndPrimitiveNdjsonSpec);
        generatedCode.Should().NotContain("IAsyncEnumerable");
        generatedCode.Should().Contain("Task<Response> GetLog(");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_For_Mixed_Json_And_Primitive_Ndjson()
    {
        string generatedCode = await GenerateCode(MixedJsonAndPrimitiveNdjsonSpec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private const string MixedRefJsonAndPrimitiveNdjsonSpec = @"
openapi: '3.0.0'
info:
  title: Task Instance Logs
  version: 1.0.0
paths:
  '/logs':
    get:
      operationId: getLog
      summary: Get Log
      responses:
        '200':
          description: Successful Response
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TaskInstancesLogResponse'
            application/x-ndjson:
              schema:
                type: string
components:
  schemas:
    TaskInstancesLogResponse:
      type: object
      properties:
        content:
          type: string
";

    [Test]
    public async Task Generates_Task_With_Ref_Schema_For_Mixed_Json_And_Primitive_Ndjson()
    {
        string generatedCode = await GenerateCode(MixedRefJsonAndPrimitiveNdjsonSpec);
        generatedCode.Should().NotContain("IAsyncEnumerable");
        generatedCode.Should().Contain("Task<TaskInstancesLogResponse> GetLog(");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_For_Mixed_Ref_Json_And_Primitive_Ndjson()
    {
        string generatedCode = await GenerateCode(MixedRefJsonAndPrimitiveNdjsonSpec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Does_Not_Generate_IAsyncEnumerable_When_Disabled(string spec)
    {
        string generatedCode = await GenerateCode(spec, settings => settings.ReturnIAsyncEnumerable = false);
        generatedCode.Should().NotContain("IAsyncEnumerable");
    }

    [Test]
    [Category("Integration")]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Can_Build_Generated_Code_When_IAsyncEnumerable_Disabled(string spec)
    {
        string generatedCode = await GenerateCode(spec, settings => settings.ReturnIAsyncEnumerable = false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(
        string spec,
        Action<RefitGeneratorSettings>? configure = null)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };
        configure?.Invoke(settings);

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        string generatedCode = sut.Generate();
        return generatedCode;
    }
}
