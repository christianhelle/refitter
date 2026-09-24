using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// deepObject style query parameters and bracketed query parameter names.
/// </summary>
public class DeepObjectQueryParameterTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: DeepObject, version: v1 }
        paths:
          /search:
            get:
              operationId: Search
              parameters:
                - name: filter
                  in: query
                  style: deepObject
                  explode: true
                  schema:
                    type: object
                    properties:
                      name: { type: string }
                      age: { type: integer }
                - name: 'page[size]'
                  in: query
                  schema: { type: integer }
                - name: 'page[number]'
                  in: query
                  schema: { type: integer }
              responses:
                '200': { description: OK }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Aliased_Bracketed_Query_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query, AliasAs(\"page[size]\")] int? pagesize");
        generatedCode.Should().Contain("[Query, AliasAs(\"page[number]\")] int? pagenumber");
        generatedCode.Should().Contain("[Query] Filter filter");
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
