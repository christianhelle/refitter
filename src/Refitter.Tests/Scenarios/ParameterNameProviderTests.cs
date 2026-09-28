using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class ParameterNameProviderTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Parameter Name Provider API'
paths:
  /pets/{pet_id}:
    get:
      operationId: 'getPet'
      parameters:
        - name: 'pet_id'
          in: 'path'
          required: true
          schema:
            type: 'integer'
        - name: 'page_size'
          in: 'query'
          schema:
            type: 'integer'
        - name: 'X-Request-Id'
          in: 'header'
          schema:
            type: 'string'
      responses:
        '200':
          description: 'A pet'
          content:
            application/json:
              schema:
                type: 'string'
";

    private sealed class PrefixingParameterNameProvider : IParameterNameProvider
    {
        public List<ParameterNameContext> Contexts { get; } = [];

        public string GetParameterName(ParameterNameContext context)
        {
            Contexts.Add(context);
            return "p" + new string(context.Name.Where(char.IsLetterOrDigit).ToArray());
        }
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(new PrefixingParameterNameProvider());
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Uses_Provided_Parameter_Names()
    {
        string generatedCode = await GenerateCode(new PrefixingParameterNameProvider());
        generatedCode.Should().Contain("int ppetid");
        generatedCode.Should().Contain("int? ppagesize");
        generatedCode.Should().Contain("string pXRequestId");
    }

    [Test]
    public async Task Passes_Parameter_Context()
    {
        var provider = new PrefixingParameterNameProvider();
        await GenerateCode(provider);

        string[] allNames = ["pet_id", "page_size", "X-Request-Id"];
        provider.Contexts.Should().ContainEquivalentOf(
            new ParameterNameContext("pet_id", ParameterSource.Path, IsRequired: true, allNames));
        provider.Contexts.Should().ContainEquivalentOf(
            new ParameterNameContext("page_size", ParameterSource.Query, IsRequired: false, allNames));
        provider.Contexts.Should().ContainEquivalentOf(
            new ParameterNameContext("X-Request-Id", ParameterSource.Header, IsRequired: false, allNames));
    }

    private static async Task<string> GenerateCode(IParameterNameProvider provider)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ParameterNameProvider = provider,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
