using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.1 type arrays that include "null" and oneOf with a null type.
/// </summary>
public class OpenApi31NullableTypeArrayTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: { title: TypeArrays, version: v1 }
        paths:
          /t:
            get:
              operationId: GetT
              parameters:
                - { name: q, in: query, schema: { type: [string, 'null'] } }
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/Thing' }
        components:
          schemas:
            Thing:
              type: object
              properties:
                name: { type: [string, 'null'] }
                count: { type: [integer, 'null'], format: int32 }
                tags: { type: [array, 'null'], items: { type: string } }
                child:
                  oneOf:
                    - { $ref: '#/components/schemas/Child' }
                    - { type: 'null' }
            Child: { type: object, properties: { id: { type: integer } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Maps_Nullable_Type_Arrays_To_CSharp_Types()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public int? Count { get; set; }");
        generatedCode.Should().Contain("public ICollection<string> Tags { get; set; }");
        generatedCode.Should().Contain("public Child Child { get; set; }");
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
