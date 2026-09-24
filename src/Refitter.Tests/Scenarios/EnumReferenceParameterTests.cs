using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Referenced string and int64 enums used as path, query and array parameters.
/// </summary>
public class EnumReferenceParameterTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: EnumRefs, version: v1 }
        paths:
          /e/{kind}:
            get:
              operationId: GetE
              parameters:
                - { name: kind, in: path, required: true, schema: { $ref: '#/components/schemas/Kind' } }
                - { name: kinds, in: query, schema: { type: array, items: { $ref: '#/components/schemas/Kind' } } }
                - { name: numKind, in: query, schema: { $ref: '#/components/schemas/NumKind' } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { type: array, items: { $ref: '#/components/schemas/Kind' } } } }
        components:
          schemas:
            Kind: { type: string, enum: [one, two] }
            NumKind: { type: integer, format: int64, enum: [10, 20] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Uses_Enum_Types_For_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<ICollection<Kind>> GetE(Kind kind, [Query(CollectionFormat.Multi)] IEnumerable<Kind> kinds, [Query] NumKind? numKind);");
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
