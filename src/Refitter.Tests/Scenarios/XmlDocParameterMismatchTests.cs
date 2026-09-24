using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Keyword-named and cookie parameters produce XML docs for parameters that are not emitted (#1263).
/// </summary>
public class XmlDocParameterMismatchTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: XmlDocParams, version: v1 }
        paths:
          /k:
            get:
              operationId: GetK
              parameters:
                - { name: class, in: query, schema: { type: string } }
                - { name: session, in: cookie, schema: { type: string } }
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1263")]
    public async Task Documents_Keyword_Parameters_Without_Escape_Prefix()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("<param name=\"class\">");
        generatedCode.Should().NotContain("<param name=\"@class\">");
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1263")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1263")]
    public async Task Can_Build_Generated_Code_With_Dynamic_Querystring_Parameters()
    {
        var generatedCode = await GenerateCode(settings => settings.UseDynamicQuerystringParameters = true);
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
