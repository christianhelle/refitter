using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Responses that are arrays of arrays, dictionaries of arrays, dictionaries of dictionaries and free-form objects.
/// </summary>
public class NestedCollectionResponseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Nested, version: v1 }
        paths:
          /m:
            get:
              operationId: GetMatrix
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { type: array, items: { type: array, items: { type: number, format: double } } }
          /d:
            get:
              operationId: GetDict
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { type: object, additionalProperties: { type: array, items: { type: string } } }
          /dd:
            get:
              operationId: GetDictOfDict
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { type: object, additionalProperties: { type: object, additionalProperties: { type: integer } } }
          /free:
            get:
              operationId: GetFreeForm
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { type: object, additionalProperties: true }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Nested_Collection_Return_Types()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<ICollection<ICollection<double>>> GetMatrix();");
        generatedCode.Should().Contain("Task<IDictionary<string, ICollection<string>>> GetDict();");
        generatedCode.Should().Contain("Task<IDictionary<string, IDictionary<string, int>>> GetDictOfDict();");
        generatedCode.Should().Contain("Task<object> GetFreeForm();");
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

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
