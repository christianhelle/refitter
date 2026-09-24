using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Unicode letters in paths, parameters, operationIds, tags and schemas, and very long operationIds.
/// </summary>
public class UnicodeIdentifierTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: 'Unicode API', version: v1 }
        paths:
          /straße/{größe}:
            get:
              operationId: holeGröße
              tags: [Größen]
              parameters:
                - { name: größe, in: path, required: true, schema: { type: string } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Größe' } } }
          /very/long:
            get:
              operationId: ThisIsAnExtremelyLongOperationIdThatKeepsGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoingAndGoing
              responses: { '204': { description: ok } }
        components:
          schemas:
            Größe: { type: object, properties: { wert: { type: string }, 'naïve-prop': { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Keeps_Unicode_Letters_In_Identifiers()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Größe> HoleGröße(string größe);");
        generatedCode.Should().Contain("public partial class Größe");
    }

    [Test]
    public async Task Keeps_Unicode_Letters_In_Tag_Interfaces()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
        generatedCode.Should().Contain("interface IGrößenApi");
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
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Tag()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
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
