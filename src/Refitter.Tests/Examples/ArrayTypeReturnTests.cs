using FluentAssertions;
using Refitter.Tests.TestUtilities;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class ArrayTypeReturnTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
  title: 'Test API'
servers:
  - url: 'https://test.host.com/api/v1'
paths:
  /jobs:
    get:
      tags:
      - 'Jobs'
      summary: 'Get jobs'
      description: 'List the job.'      
      responses:
        '200':
          description: 'successful operation'
          content:
            application/json:
              schema:
                type: 'array'
                items:
                  $ref: '#/components/schemas/JobResponse'
components:
  schemas:
    JobResponse:
      type: 'object'
      properties:
        job:
          type: 'object'
          properties:
            start-date:
              type: 'string'
              format: 'date-time'
            details:
              type: 'string'
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Returns_IList()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IList<JobResponse>");
    }

    [Fact]
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
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings { ArrayType = "System.Collections.Generic.IList" }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
