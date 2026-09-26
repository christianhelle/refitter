using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Enum values that differ only by case generate duplicate members (#1267).
/// </summary>
public class EnumCaseCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: EnumCase, version: v1 }
        paths:
          /e:
            get:
              operationId: GetE
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/Mode' } } } } }
        components:
          schemas:
            Mode: { type: string, enum: [a, A] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Unique_Member_Names_And_Keeps_Values()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().MatchRegex(@"Value = @""a""\)\]\s*A = 0,");
        generatedCode.Should().MatchRegex(@"Value = @""A""\)\]\s*A2 = 1,");
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
