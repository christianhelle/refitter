using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Enum values that are empty, signed, prefixed with symbols, keywords, unicode, numeric or boolean.
/// </summary>
public class EnumValueEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Enums, version: v1 }
        paths:
          /e:
            get:
              operationId: GetE
              parameters:
                - { name: sort, in: query, schema: { type: string, enum: ['+name', '-name', 'name asc', 'name desc'] } }
                - { name: level, in: query, schema: { type: integer, enum: [-1, 0, 1, 2] } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/E' } } }
        components:
          schemas:
            E:
              type: object
              properties:
                status: { type: string, enum: ['', 'a', 'b-c', 'b_c', '1st', 'class', 'null'] }
                num: { type: number, enum: [1.5, 2.5] }
                flag: { type: boolean, enum: [true, false] }
                mixed: { type: string, enum: [open, closed], nullable: true }
                unicode: { type: string, enum: ['über', 'naïve', '日本'] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Sanitizes_Enum_Member_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Plusname = 0,");
        generatedCode.Should().Contain("Minusname = 1,");
        generatedCode.Should().Contain("Name_asc = 2,");
        generatedCode.Should().Contain("__1 = -1,");
        generatedCode.Should().Contain("Empty = 0,");
        generatedCode.Should().Contain("_1st = 4,");
        generatedCode.Should().Contain("Class = 5,");
        generatedCode.Should().Contain("Null = 6,");
        generatedCode.Should().Contain("Über = 0,");
        generatedCode.Should().Contain("日本 = 2,");
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
    public async Task Can_Build_Generated_Code_With_Optional_Parameters()
    {
        var generatedCode = await GenerateCode(settings => settings.OptionalParameters = true);
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
