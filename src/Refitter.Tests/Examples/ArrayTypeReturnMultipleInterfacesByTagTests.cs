using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class ArrayTypeReturnMultipleInterfacesByTagTests
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
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Returns_IList()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("IList<JobResponse>");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings 
        { 
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            CodeGeneratorSettings = new CodeGeneratorSettings { ArrayType = "System.Collections.Generic.IList" }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
