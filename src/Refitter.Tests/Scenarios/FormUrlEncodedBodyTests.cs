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
    public async Task Serializes_Form_Body_As_UrlEncoded()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Body(BodySerializationMethod.UrlEncoded)] Body body");
    }

    [Test]
    public async Task Serializes_Form_Body_With_Charset_As_UrlEncoded()
    {
        var generatedCode = await GenerateCode(
            OpenApiSpec.Replace(
                "application/x-www-form-urlencoded:",
                "'application/x-www-form-urlencoded; charset=utf-8':"));
        generatedCode.Should().Contain("[Body(BodySerializationMethod.UrlEncoded)] Body body");
    }

    [Test]
    public async Task Keeps_Default_Body_For_Other_Media_Types_With_The_Same_Prefix()
    {
        var generatedCode = await GenerateCode(
            OpenApiSpec.Replace(
                "application/x-www-form-urlencoded:",
                "application/x-www-form-urlencoded-extra:"));
        generatedCode.Should().Contain("[Body] Body body");
        generatedCode.Should().NotContain("BodySerializationMethod.UrlEncoded");
    }

    [Test]
    [Arguments("APPLICATION/X-WWW-FORM-URLENCODED")]
    [Arguments("'application/x-www-form-urlencoded ; charset=utf-8'")]
    public async Task Matches_Form_Media_Type_Case_Insensitively_And_Ignoring_Parameters(string mediaType)
    {
        var generatedCode = await GenerateCode(
            OpenApiSpec.Replace("application/x-www-form-urlencoded:", $"{mediaType}:"));
        generatedCode.Should().Contain("[Body(BodySerializationMethod.UrlEncoded)] Body body");
    }

    [Test]
    public async Task Keeps_Json_Body_When_Json_Is_Also_Accepted()
    {
        var generatedCode = await GenerateCode(
            OpenApiSpec.Replace(
                "application/x-www-form-urlencoded:",
                "application/json: { schema: { type: object, properties: { grant_type: { type: string } } } }\n"
                + "          application/x-www-form-urlencoded:"));
        generatedCode.Should().Contain("Content-Type: application/json");
        generatedCode.Should().Contain("[Body] Body body");
        generatedCode.Should().NotContain("BodySerializationMethod.UrlEncoded");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec = OpenApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
