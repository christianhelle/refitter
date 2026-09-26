using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Deprecated schemas used in operation signatures raise CS0612 in the generated code (#1278).
/// </summary>
public class DeprecatedSchemaTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Deprecated, version: v1 }
        paths:
          /d:
            get:
              operationId: GetD
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/D' } } } } }
          /old:
            get:
              operationId: GetOld
              deprecated: true
              responses: { '200': { description: ok, content: { application/json: { schema: { type: array, items: { $ref: '#/components/schemas/OldItem' } } } } } }
        components:
          schemas:
            D:
              type: object
              deprecated: true
              properties:
                old: { type: string }
                legacy: { $ref: '#/components/schemas/Legacy' }
            OldItem:
              type: object
              deprecated: true
              properties:
                id: { type: integer }
            Legacy:
              type: object
              deprecated: true
              properties:
                value: { type: string }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Removes_Obsolete_From_Schemas_Used_By_Interface()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotMatchRegex(@"\[System\.Obsolete\]\s*public partial class (D|OldItem)\b");
        generatedCode.Should().Contain("public partial class D");
        generatedCode.Should().Contain("public partial class OldItem");
    }

    [Test]
    public async Task Keeps_Obsolete_On_Schemas_Not_Used_By_Interface()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().MatchRegex(@"\[System\.Obsolete\]\s*public partial class Legacy\b");
    }

    [Test]
    public async Task Keeps_Obsolete_On_Deprecated_Operations()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().MatchRegex(@"\[System\.Obsolete\]\s*(\[[^\]]*\]\s*)*\[Get\(""/old""\)\]");
    }

    [Test]
    public async Task Keeps_Obsolete_When_Clients_Are_Not_Generated()
    {
        var generatedCode = await GenerateCode(settings => settings.GenerateClients = false);
        generatedCode.Should().MatchRegex(@"\[System\.Obsolete\]\s*public partial class D\b");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Multiple_Generated_Files()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, GenerateMultipleFiles = true };
        var sut = await RefitGenerator.CreateAsync(settings);
        var files = sut.GenerateMultipleFiles().Files.Select(f => f.Content).ToArray();

        BuildHelper.BuildCSharp(files).Should().BeTrue();
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
