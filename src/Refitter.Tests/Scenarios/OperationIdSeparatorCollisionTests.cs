using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// operationIds that collide once casing and separators are normalized.
/// </summary>
public class OperationIdSeparatorCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: OpIdCase, version: v1 }
        paths:
          /a:
            get:
              operationId: getItems
              responses: { '204': { description: ok } }
          /b:
            get:
              operationId: GetItems
              responses: { '204': { description: ok } }
          /c:
            get:
              operationId: get-items
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Distinct_Method_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task A();");
        generatedCode.Should().Contain("Task B();");
        generatedCode.Should().Contain("Task C();");
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
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Endpoint()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint);
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
