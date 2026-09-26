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
    public async Task Generates_System_Net_Http_Using()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("using System.Net.Http;");
    }

    [Test]
    public async Task Generates_System_Net_Http_Using_In_Each_File()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateMultipleFiles = true,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var interfaceFile = sut.GenerateMultipleFiles().Files.First(f => f.Content.Contains("HttpResponseMessage"));
        interfaceFile.Content.Should().Contain("using System.Net.Http;");
    }

    [Test]
    public async Task Does_Not_Generate_System_Net_Http_Using_Without_HttpResponseMessage()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(
            OpenApiSpec.Replace("application/octet-stream: { schema: { type: string, format: binary } }", "application/json: { schema: { type: string } }"));
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        generatedCode.Should().NotContain("HttpResponseMessage");
        generatedCode.Should().NotContain("using System.Net.Http;");
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
    public async Task Can_Build_Generated_Code_Without_Implicit_Usings()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharpWithoutImplicitUsings(generatedCode).Should().BeTrue();
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
