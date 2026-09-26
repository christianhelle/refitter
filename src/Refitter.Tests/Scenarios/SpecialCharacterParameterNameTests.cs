using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Parameter names with dots, leading digits, $, @, spaces, underscores and unicode letters.
/// </summary>
public class SpecialCharacterParameterNameTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: ParamNames, version: v1 }
        paths:
          /p/{item.id}/{2nd}:
            get:
              operationId: GetP
              parameters:
                - { name: item.id, in: path, required: true, schema: { type: string } }
                - { name: 2nd, in: path, required: true, schema: { type: string } }
                - { name: $top, in: query, schema: { type: integer } }
                - { name: '@type', in: query, schema: { type: string } }
                - { name: 'x-request-id', in: header, schema: { type: string } }
                - { name: 'naïve', in: query, schema: { type: string } }
                - { name: 'a b', in: query, schema: { type: string } }
                - { name: '_', in: query, schema: { type: string } }
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Sanitizes_Parameter_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[AliasAs(\"item.id\")] string item_id");
        generatedCode.Should().Contain("[AliasAs(\"2nd\")] string _2nd");
        generatedCode.Should().Contain("[Query, AliasAs(\"$top\")] int? top");
        generatedCode.Should().Contain("[Query, AliasAs(\"@type\")] string type");
        generatedCode.Should().Contain("[Query, AliasAs(\"a b\")] string a_b");
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
