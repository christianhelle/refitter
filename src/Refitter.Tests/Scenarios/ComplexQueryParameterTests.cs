using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Query arrays of enums, delimited styles, object and map parameters and content-encoded parameters.
/// </summary>
public class ComplexQueryParameterTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: QueryComplex, version: v1 }
        paths:
          /q:
            get:
              operationId: GetQ
              parameters:
                - { name: statuses, in: query, explode: false, schema: { type: array, items: { type: string, enum: [a, b] } } }
                - { name: ids, in: query, style: pipeDelimited, schema: { type: array, items: { type: integer } } }
                - { name: tags, in: query, style: spaceDelimited, schema: { type: array, items: { type: string } } }
                - { name: formExploded, in: query, style: form, explode: true, schema: { type: array, items: { type: string } } }
                - { name: formStyled, in: query, style: form, schema: { type: array, items: { type: string } } }
                - { name: plain, in: query, schema: { type: array, items: { type: string } } }
                - { name: explodedOnly, in: query, explode: true, schema: { type: array, items: { type: string } } }
                - { name: formCsv, in: query, style: form, explode: false, schema: { type: array, items: { type: string } } }
                - { name: deepList, in: query, style: deepObject, explode: true, schema: { type: array, items: { type: string } } }
                - { name: obj, in: query, schema: { $ref: '#/components/schemas/Filter' } }
                - { name: map, in: query, schema: { type: object, additionalProperties: { type: string } } }
                - { name: content, in: query, content: { application/json: { schema: { $ref: '#/components/schemas/Filter' } } } }
              responses: { '204': { description: ok } }
        components:
          schemas:
            Filter: { type: object, properties: { name: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Complex_Query_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IEnumerable<int> ids");
        generatedCode.Should().Contain("[Query] Filter obj");
        generatedCode.Should().Contain("[Query] IDictionary<string, string> map");
    }

    [Test]
    public async Task Uses_Collection_Format_From_Parameter_Style()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)] IEnumerable<Anonymous> statuses");
        generatedCode.Should().Contain("[Query(CollectionFormat.Pipes)] IEnumerable<int> ids");
        generatedCode.Should().Contain("[Query(CollectionFormat.Ssv)] IEnumerable<string> tags");
        generatedCode.Should().Contain("[Query(CollectionFormat.Multi)] IEnumerable<string> formExploded");
        generatedCode.Should().Contain("[Query(CollectionFormat.Multi)] IEnumerable<string> formStyled");
        generatedCode.Should().Contain("[Query(CollectionFormat.Multi)] IEnumerable<string> explodedOnly");
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)] IEnumerable<string> formCsv");
    }

    [Test]
    public async Task Parameter_Style_Takes_Precedence_Over_Collection_Format_Setting()
    {
        var generatedCode = await GenerateCode(settings => settings.CollectionFormat = CollectionFormat.Tsv);
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)] IEnumerable<Anonymous> statuses");
        generatedCode.Should().Contain("[Query(CollectionFormat.Multi)] IEnumerable<string> formExploded");
        generatedCode.Should().Contain("[Query(CollectionFormat.Tsv)] IEnumerable<string> plain");
        generatedCode.Should().Contain("[Query(CollectionFormat.Tsv)] IEnumerable<string> deepList");
    }

    [Test]
    public async Task Uses_Collection_Format_From_Parameter_Style_With_Dynamic_Querystring_Parameters()
    {
        var generatedCode = await GenerateCode(settings => settings.UseDynamicQuerystringParameters = true);
        generatedCode.Should().Contain("[Query(CollectionFormat.Pipes)]");
        generatedCode.Should().Contain("[Query(CollectionFormat.Ssv)]");
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)]");
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
