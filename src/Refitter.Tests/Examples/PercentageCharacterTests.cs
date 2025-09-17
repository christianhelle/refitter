using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class PercentageCharacterTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
  title: 'Test API'
servers:
  - url: 'https://test.host.com/api/v1'
paths:
  /data:
    post:
      requestBody:
        content:
          application/json:
            schema:
              type: 'object'
              properties:
                '% of something':
                  type: 'string'
      responses:
        '200':
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SomeResponse'
components:
  schemas:
    SomeResponse:
      type: 'object'
      properties:
        data:
          type: 'object'
          properties:
            id:
              type: 'string'
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
    public async Task Replaces_Percentage_With_PascalCase()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("string PercentOfSomething");
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
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
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
