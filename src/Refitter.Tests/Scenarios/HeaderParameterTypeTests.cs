using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Typed header parameters and headers named like standard HTTP headers.
/// </summary>
public class HeaderParameterTypeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Headers, version: v1 }
        paths:
          /h:
            get:
              operationId: GetH
              parameters:
                - { name: X-Int, in: header, required: true, schema: { type: integer } }
                - { name: X-Array, in: header, schema: { type: array, items: { type: string } } }
                - { name: X-Date, in: header, schema: { type: string, format: date-time } }
                - { name: X-Enum, in: header, schema: { type: string, enum: [a, b] } }
                - { name: Accept, in: header, schema: { type: string } }
                - { name: Content-Type, in: header, schema: { type: string } }
                - { name: Authorization, in: header, schema: { type: string } }
              responses:
                '200':
                  description: ok
                  headers:
                    X-Rate-Limit: { schema: { type: integer } }
                  content: { application/json: { schema: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Typed_Header_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Header(\"X-Int\")] int x_Int");
        generatedCode.Should().Contain("[Header(\"X-Array\")] IEnumerable<string> x_Array");
        generatedCode.Should().Contain("[Header(\"X-Date\")] System.DateTimeOffset? x_Date");
        generatedCode.Should().Contain("[Header(\"X-Enum\")] XEnum? x_Enum");
        generatedCode.Should().Contain("[Header(\"Authorization\")] string authorization");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Cancellation_Tokens()
    {
        var generatedCode = await GenerateCode(settings => settings.UseCancellationTokens = true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
