using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Self-referencing and mutually recursive schemas.
/// </summary>
public class RecursiveSchemaTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Recursive, version: v1 }
        paths:
          /tree:
            get:
              operationId: GetTree
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Node' } } }
          /a:
            get:
              operationId: GetA
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/A' } } }
        components:
          schemas:
            Node:
              type: object
              properties:
                children: { type: array, items: { $ref: '#/components/schemas/Node' } }
                parent: { $ref: '#/components/schemas/Node' }
                byName: { type: object, additionalProperties: { $ref: '#/components/schemas/Node' } }
            A: { type: object, properties: { b: { $ref: '#/components/schemas/B' } } }
            B: { type: object, properties: { a: { $ref: '#/components/schemas/A' } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Recursive_Contracts()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public ICollection<Node> Children { get; set; }");
        generatedCode.Should().Contain("public Node Parent { get; set; }");
        generatedCode.Should().Contain("public IDictionary<string, Node> ByName { get; set; }");
        generatedCode.Should().Contain("public B B { get; set; }");
        generatedCode.Should().Contain("public A A { get; set; }");
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
