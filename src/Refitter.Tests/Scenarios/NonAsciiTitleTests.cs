using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Non-ASCII punctuation in info.title must not leak into the interface name (#1271).
/// </summary>
public class NonAsciiTitleTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: 'Pet API — «v2»', version: v1 }
        paths:
          /p:
            get:
              operationId: GetPet
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1271")]
    public async Task Removes_Invalid_Characters_From_Interface_Name()
    {
        var generatedCode = await GenerateCode();
        var interfaceName = System.Text.RegularExpressions.Regex
            .Match(generatedCode, @"interface\s+(\S+)")
            .Groups[1]
            .Value;
        interfaceName.Should().MatchRegex(@"^[\p{L}\p{Nd}_]+$");
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1271")]
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
