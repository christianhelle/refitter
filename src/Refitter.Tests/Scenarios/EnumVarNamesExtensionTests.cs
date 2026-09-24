using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Integer enums named through x-enum-varnames and x-enumNames.
/// </summary>
public class EnumVarNamesExtensionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: EnumVarNames, version: v1 }
        paths:
          /e:
            get:
              operationId: GetE
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Priority' } } }
        components:
          schemas:
            Priority:
              type: integer
              enum: [0, 1, 2]
              x-enum-varnames: [Low, Medium, High]
              x-enumNames: [Low, Medium, High]
              x-enum-descriptions: [low, med, high]
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Uses_Enum_Var_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Low = 0,");
        generatedCode.Should().Contain("Medium = 1,");
        generatedCode.Should().Contain("High = 2,");
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
