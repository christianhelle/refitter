using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// 2XX/4XX wildcard status codes and operations that only define a default response.
/// </summary>
public class WildcardStatusCodeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Wildcard, version: v1 }
        paths:
          /w:
            get:
              operationId: GetW
              responses:
                '2XX':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/W' } } }
                '4XX':
                  description: err
                  content: { application/json: { schema: { $ref: '#/components/schemas/Err' } } }
                default:
                  description: err
          /only-default:
            get:
              operationId: GetOnlyDefault
              responses:
                default:
                  description: whatever
                  content: { application/json: { schema: { $ref: '#/components/schemas/W' } } }
        components:
          schemas:
            W: { type: object, properties: { a: { type: string } } }
            Err: { type: object, properties: { m: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Uses_Wildcard_And_Default_Responses_As_Return_Type()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<W> GetW();");
        generatedCode.Should().Contain("Task<W> GetOnlyDefault();");
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
    public async Task Can_Build_Generated_Code_With_IApiResponse()
    {
        var generatedCode = await GenerateCode(settings => settings.ReturnIApiResponse = true);
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
