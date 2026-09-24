using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Tags with spaces, leading digits, keywords, empty names and case/separator variations.
/// </summary>
public class TagNameEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Tags, version: v1 }
        paths:
          /a:
            get:
              operationId: GetA
              tags: ['user management', 'Admin']
              responses: { '204': { description: ok } }
          /b:
            get:
              operationId: GetB
              tags: ['123-numeric']
              responses: { '204': { description: ok } }
          /c:
            get:
              operationId: GetC
              tags: ['class']
              responses: { '204': { description: ok } }
          /d:
            get:
              operationId: GetD
              responses: { '204': { description: ok } }
          /e:
            get:
              operationId: GetE
              tags: ['User-Management']
              responses: { '204': { description: ok } }
          /f:
            get:
              operationId: GetF
              tags: ['']
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Valid_Interface_Names_From_Tags()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
        generatedCode.Should().Contain("interface IUsermanagementApi");
        generatedCode.Should().Contain("interface I_123numericApi");
        generatedCode.Should().Contain("interface IClassApi");
        generatedCode.Should().Contain("interface IUserManagementApi");
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
