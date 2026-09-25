using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.1 jsonSchemaDialect, license identifier, $ref siblings, prefixItems, if/then/else and contentMediaType.
/// </summary>
public class OpenApi31AdvancedKeywordTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: { title: Adv31, version: v1, summary: A summary, license: { name: MIT, identifier: MIT } }
        jsonSchemaDialect: 'https://spec.openapis.org/oas/3.1/dialect/base'
        paths:
          /x:
            get:
              operationId: GetX
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/X'
                        description: sibling description allowed in 3.1
        components:
          pathItems:
            ItemPath:
              get:
                operationId: GetItem
                parameters: [ { name: id, in: path, required: true, schema: { type: string } } ]
                responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/X' } } } } }
          schemas:
            X:
              type: object
              properties:
                tuple: { type: array, prefixItems: [ { type: string }, { type: integer } ] }
                maybe: { anyOf: [ { type: string }, { type: 'null' } ] }
                cond:
                  type: object
                  if: { properties: { kind: { const: a } } }
                  then: { properties: { a: { type: string } } }
                  else: { properties: { b: { type: string } } }
                bin: { type: string, contentMediaType: image/png, contentEncoding: base64 }
                dep: { type: string, deprecated: true }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Contracts_For_OpenApi31_Keywords()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<X> GetX();");
        generatedCode.Should().Contain("public ICollection<object> Tuple { get; set; }");
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
