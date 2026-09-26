using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// TRACE operations cannot be expressed with Refit attributes (#1265).
/// </summary>
public class TraceOperationTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Trace, version: v1 }
        paths:
          /r:
            trace:
              operationId: TraceR
              responses: { '200': { description: ok } }
            get:
              operationId: GetR
              responses: { '200': { description: ok } }
        """;

    private const string TraceOnlySpec = """
        openapi: 3.0.1
        info: { title: TraceOnly, version: v1 }
        paths:
          /r:
            trace:
              operationId: TraceR
              tags: [ Diagnostics ]
              responses: { '200': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Does_Not_Generate_Unknown_Trace_Attribute()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("[Trace(");
    }

    [Test]
    public async Task Skips_Trace_Operation()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("TraceR");
    }

    [Test]
    public async Task Generates_Other_Operations_On_Same_Path()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Get(\"/r\")]");
        generatedCode.Should().Contain("Task GetR(");
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
    public async Task Can_Build_Generated_Code_ByEndpoint_With_Dependency_Injection()
    {
        var generatedCode = await GenerateCode(settings =>
        {
            settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint;
            settings.DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = "https://example.com" };
        });
        generatedCode.Should().NotContain("TraceR");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Arguments(MultipleInterfaces.Unset)]
    [Arguments(MultipleInterfaces.ByEndpoint)]
    [Arguments(MultipleInterfaces.ByTag)]
    public async Task Can_Generate_Code_When_All_Operations_Are_Trace(MultipleInterfaces multipleInterfaces)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(TraceOnlySpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = multipleInterfaces,
            DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = "https://example.com" },
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotContain("TraceR");
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
