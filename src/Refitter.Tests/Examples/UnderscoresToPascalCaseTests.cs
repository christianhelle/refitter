using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class HandleUnderscoresTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
  title: 'Test API'
servers:
  - url: 'https://test.host.com/api/v1'
paths:
  /jobs/{jobId}:
    get:
      tags:
      - 'Jobs'
      summary: 'Get job details'
      description: 'Get the details of the specified job.'
      parameters:
        - in: 'path'
          name: 'jobId'
          description: 'Job ID'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/JobResponse'
components:
  schemas:
    JobResponse:
      type: 'object'
      properties:
        job:
          type: 'object'
          properties:
            start_date:
              type: 'string'
              format: 'date-time'
            details:
              type: 'string'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Replaces_Underscore_Properties_With_PascalCase()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("DateTimeOffset StartDate");
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
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
