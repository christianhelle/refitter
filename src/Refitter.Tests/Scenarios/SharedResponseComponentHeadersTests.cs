using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;


public class SharedResponseComponentHeadersTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
info:
  title: Shared Responses
  version: 1.0.0
paths:
  /plain:
    get:
      operationId: getPlain
      responses:
        '200':
          $ref: '#/components/responses/PlainJson'
  /events:
    get:
      operationId: getEvents
      responses:
        '200':
          $ref: '#/components/responses/EventStream'
components:
  responses:
    PlainJson:
      description: shared json response
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Event'
    EventStream:
      description: shared streaming response
      content:
        text/event-stream:
          schema:
            type: array
            items:
              $ref: '#/components/schemas/Event'
  schemas:
    Event:
      type: object
      properties:
        id:
          type: integer
          format: int64
";

    [Test]
    public async Task Generates_Accept_Header_For_Referenced_Response()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Accept: application/json");
    }

    [Test]
    public async Task Generates_Accept_Header_For_Referenced_Streaming_Response()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Accept: text/event-stream");
    }

    [Test]
    public async Task Generates_IAsyncEnumerable_For_Referenced_Streaming_Response()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IAsyncEnumerable<Event> GetEvents(");
    }

    [Test]
    [Category("Integration")]
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
            OpenApiPath = swaggerFile,
            AddAcceptHeaders = true
        };

        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
