using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class Int32FormatWithPatternTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.4
info:
  version: '1.0.0'
  title: 'WeatherForecast API'
servers:
  - url: 'https://api.example.com/v1'
paths:
  /weatherforecast:
    get:
      operationId: 'getWeatherForecast'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: 'array'
                items:
                  $ref: '#/components/schemas/WeatherForecast'
components:
  schemas:
    WeatherForecast:
      type: object
      properties:
        date:
          type: string
          format: date
        temperatureC:
          pattern: '^-?(?:0|[1-9]\\d*)$'
          format: int32
        temperatureF:
          pattern: '^-?(?:0|[1-9]\\d*)$'
          format: int32
        summary:
          type: string
          nullable: true
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Generate_Int_For_Properties_With_Format_Int32_And_Pattern()
    {
        string generatedCode = await GenerateCode();
        
        // Should generate int, not object
        generatedCode.Should().Contain("public int TemperatureC { get; set; }");
        generatedCode.Should().Contain("public int TemperatureF { get; set; }");
        
        // Should not generate as object
        generatedCode.Should().NotContain("public object TemperatureC { get; set; }");
        generatedCode.Should().NotContain("public object TemperatureF { get; set; }");
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
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
