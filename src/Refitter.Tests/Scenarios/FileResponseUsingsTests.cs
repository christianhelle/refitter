using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// File responses return HttpResponseMessage, which needs System.Net.Http without implicit usings (#1272).
/// </summary>
public class FileResponseUsingsTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Download, version: v1 }
        paths:
          /f:
            get:
              operationId: Download
              responses: { '200': { description: ok, content: { application/octet-stream: { schema: { type: string, format: binary } } } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1272")]
    public async Task Generates_System_Net_Http_Using()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("using System.Net.Http;");
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
