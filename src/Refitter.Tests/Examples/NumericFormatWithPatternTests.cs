using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Examples
{
    public class NumericFormatWithPatternTests
    {
        private const string OpenApiSpec = @"
openapi: 3.0.4
info:
  version: '1.0.0'
  title: 'Sensor Data API'
servers:
  - url: 'https://api.example.com/v1'
paths:
  /sensor:
    get:
      operationId: 'getSensorData'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/SensorData'
components:
  schemas:
    SensorData:
      type: object
      properties:
        temperature:
          pattern: '^-?\\d+\\.\\d+$'
          format: float
        humidity:
          pattern: '^-?\\d+\\.\\d+$'
          format: double
        pressure:
          pattern: '^-?(?:0|[1-9]\\d*)$'
          format: int64
";

        [Test]
        public async Task Can_Generate_Code()
        {
            string generatedCode = await GenerateCode();
            generatedCode.Should().NotBeNullOrWhiteSpace();
        }

        [Test]
        public async Task Should_Generate_Float_For_Properties_With_Format_Float_And_Pattern()
        {
            string generatedCode = await GenerateCode();

            // Should generate float, not object
            generatedCode.Should().Contain("public float Temperature { get; set; }");

            // Should not generate as object
            generatedCode.Should().NotContain("public object Temperature { get; set; }");
        }

        [Test]
        public async Task Should_Generate_Double_For_Properties_With_Format_Double_And_Pattern()
        {
            string generatedCode = await GenerateCode();

            // Should generate double, not object
            generatedCode.Should().Contain("public double Humidity { get; set; }");

            // Should not generate as object
            generatedCode.Should().NotContain("public object Humidity { get; set; }");
        }

        [Test]
        public async Task Should_Generate_Long_For_Properties_With_Format_Int64_And_Pattern()
        {
            string generatedCode = await GenerateCode();

            // Should generate long, not object
            generatedCode.Should().Contain("public long Pressure { get; set; }");

            // Should not generate as object
            generatedCode.Should().NotContain("public object Pressure { get; set; }");
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
}
