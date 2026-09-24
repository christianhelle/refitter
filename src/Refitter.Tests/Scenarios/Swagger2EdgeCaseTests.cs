using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Swagger 2.0 file uploads, form data, collection formats, x-nullable, body arrays and file responses.
/// </summary>
public class Swagger2EdgeCaseTests
{
    private const string OpenApiSpec = """
        swagger: '2.0'
        info: { title: Swagger2, version: v1 }
        basePath: /api/v1
        consumes: [application/json]
        produces: [application/json]
        paths:
          /upload:
            post:
              operationId: Upload
              consumes: [multipart/form-data]
              parameters:
                - { name: file, in: formData, type: file, required: true }
                - { name: files, in: formData, type: array, items: { type: file } }
                - { name: meta, in: formData, type: string }
              responses: { '204': { description: ok } }
          /form:
            post:
              operationId: PostForm
              consumes: [application/x-www-form-urlencoded]
              parameters:
                - { name: a-b, in: formData, type: string }
                - { name: c, in: formData, type: integer, required: true }
              responses: { '204': { description: ok } }
          /items:
            get:
              operationId: GetItems
              parameters:
                - { name: ids, in: query, type: array, collectionFormat: multi, items: { type: integer } }
                - { name: csv, in: query, type: array, collectionFormat: csv, items: { type: string } }
                - { name: tsv, in: query, type: array, collectionFormat: tsv, items: { type: string } }
              responses:
                '200': { description: ok, schema: { type: array, items: { $ref: '#/definitions/Item' } } }
                default: { description: err, schema: { $ref: '#/definitions/Error' } }
          /body:
            put:
              operationId: PutBody
              parameters:
                - { name: payload, in: body, required: true, schema: { type: array, items: { $ref: '#/definitions/Item' } } }
              responses: { '200': { description: ok, schema: { type: file } } }
        definitions:
          Item:
            type: object
            x-nullable: true
            properties:
              id: { type: integer, x-nullable: true }
              kind: { type: string, enum: [a, b], x-enum-varnames: [Alpha, Beta] }
          Error: { type: object, properties: { message: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Swagger2_Operations()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<HttpResponseMessage> PutBody([Body] IEnumerable<Item> payload);");
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
    public async Task Can_Build_Generated_Code_With_IApiResponse()
    {
        var generatedCode = await GenerateCode(settings => settings.ReturnIApiResponse = true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Tag()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
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
