using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// application/x-www-form-urlencoded request body with enums, arrays and hyphenated names.
/// </summary>
public class FormUrlEncodedBodyTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: FormComplex, version: v1 }
        paths:
          /token:
            post:
              operationId: Token
              requestBody:
                required: true
                content:
                  application/x-www-form-urlencoded:
                    schema:
                      type: object
                      required: [grant_type]
                      properties:
                        grant_type: { type: string, enum: [client_credentials, password] }
                        scope: { type: array, items: { type: string } }
                        client-id: { type: string }
              responses:
                '200': { description: ok, content: { application/json: { schema: { type: object, properties: { access_token: { type: string } } } } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1276")]
    public async Task Serializes_Form_Body_As_UrlEncoded()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Body(BodySerializationMethod.UrlEncoded)] Body body");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
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
