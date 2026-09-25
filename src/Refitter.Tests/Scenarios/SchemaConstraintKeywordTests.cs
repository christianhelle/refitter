using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// uniqueItems, min/max constraints, patterns with escapes, int64 extremes, not and additionalProperties false.
/// </summary>
public class SchemaConstraintKeywordTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Constraints, version: v1 }
        paths:
          /c:
            post:
              operationId: PostC
              requestBody:
                content: { application/json: { schema: { $ref: '#/components/schemas/C' } } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { type: array, uniqueItems: true, items: { type: string } } } }
        components:
          schemas:
            C:
              type: object
              additionalProperties: false
              required: [tags]
              properties:
                tags: { type: array, uniqueItems: true, minItems: 1, maxItems: 10, items: { type: string, minLength: 1, maxLength: 5, pattern: '^[a-z"\\]+$' } }
                num: { type: integer, minimum: -10, maximum: 10, multipleOf: 2 }
                big: { type: integer, format: int64, minimum: -9223372036854775808, maximum: 9223372036854775807 }
                dbl: { type: number, minimum: 0.0001 }
                notString: { not: { type: string } }
                extra: { type: object, additionalProperties: { $ref: '#/components/schemas/C' } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Constrained_Properties()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public long Big { get; set; }");
        generatedCode.Should().Contain("public object NotString { get; set; }");
        generatedCode.Should().Contain("public IDictionary<string, C> Extra { get; set; }");
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
    public async Task Can_Build_Generated_Code_With_Immutable_Records()
    {
        var generatedCode = await GenerateCode(settings => settings.ImmutableRecords = true);
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
