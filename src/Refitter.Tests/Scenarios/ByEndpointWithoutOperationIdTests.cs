using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// ByEndpoint interface names collide when operations have no operationId (#1266).
/// </summary>
public class ByEndpointWithoutOperationIdTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: NoOpIds, version: v1 }
        paths:
          /users:
            get:
              responses: { '204': { description: ok } }
          /users/{id}:
            get:
              parameters: [ { name: id, in: path, required: true, schema: { type: integer } } ]
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1266")]
    public async Task Generates_Unique_Interface_Names()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint);
        System.Text.RegularExpressions.Regex
            .Matches(generatedCode, @"interface IUsersGetEndpoint\b")
            .Count
            .Should()
            .Be(1);
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
    [Skip("https://github.com/christianhelle/refitter/issues/1266")]
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
