using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A schema named like the generated interface collides with it (#1270).
/// </summary>
public class InterfaceNameCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Pet, version: v1 }
        paths:
          /p:
            get:
              operationId: GetPet
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/IPet' } } } } }
        components:
          schemas:
            IPet: { type: object, properties: { a: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Renames_Interface_That_Collides_With_Contract()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public partial interface IPet2");
        generatedCode.Should().Contain("public partial class IPet");
        generatedCode.Should().Contain("Task<IPet> GetPet(");
    }

    [Test]
    public async Task Keeps_Interface_Name_When_Contract_Type_Suffix_Avoids_Collision()
    {
        var generatedCode = await GenerateCode(settings => settings.ContractTypeSuffix = "Dto");
        generatedCode.Should().Contain("public partial interface IPet\r\n".Replace("\r\n", Environment.NewLine));
        generatedCode.Should().Contain("public partial class IPetDto");
    }

    [Test]
    public async Task Renames_Interface_That_Collides_With_Suffixed_Contract()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec.Replace("IPet", "I"));
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, ContractTypeSuffix = "Pet" };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("public partial class IPet");
        generatedCode.Should().Contain("public partial interface IPet2");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_ByTag_With_Dependency_Injection()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(
            OpenApiSpec
                .Replace("operationId: GetPet", "operationId: GetPet\n      tags: [ Pets ]")
                .Replace("IPet", "IPetsApi"));
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = "https://example.com" },
        };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("public partial interface IPets2Api");
        generatedCode.Should().Contain("AddRefitClient<IPets2Api>");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
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
