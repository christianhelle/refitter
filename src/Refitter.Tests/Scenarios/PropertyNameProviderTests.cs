using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class PropertyNameProviderTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Property Name Provider API'
paths:
  /people:
    get:
      operationId: 'getPeople'
      responses:
        '200':
          description: 'People'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Person'
components:
  schemas:
    Person:
      type: 'object'
      required:
        - 'first_name'
      properties:
        first_name:
          type: 'string'
        last_name:
          type: 'string'
        lastname:
          type: 'string'
";

    private sealed class PrefixingPropertyNameProvider : IPropertyNameProvider
    {
        public List<PropertyNameContext> Contexts { get; } = [];

        public string GetPropertyName(PropertyNameContext context)
        {
            Contexts.Add(context);
            return "Custom" + context.Name.Replace("_", string.Empty);
        }
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(new PrefixingPropertyNameProvider());
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Uses_Provided_Property_Names()
    {
        string generatedCode = await GenerateCode(new PrefixingPropertyNameProvider());
        generatedCode.Should().Contain("public string Customfirstname { get; set; }");
        generatedCode.Should().Contain("[JsonPropertyName(\"first_name\")]");
    }

    [Test]
    public async Task Deduplicates_Colliding_Provided_Names()
    {
        string generatedCode = await GenerateCode(new PrefixingPropertyNameProvider());
        generatedCode.Should().Contain("public string Customlastname { get; set; }");
        generatedCode.Should().Contain("public string Customlastname2 { get; set; }");
    }

    [Test]
    public async Task Passes_Property_Context()
    {
        var provider = new PrefixingPropertyNameProvider();
        await GenerateCode(provider);
        provider.Contexts.Should().ContainEquivalentOf(new PropertyNameContext("first_name", IsRequired: true));
        provider.Contexts.Should().ContainEquivalentOf(new PropertyNameContext("last_name", IsRequired: false));
    }

    private static async Task<string> GenerateCode(IPropertyNameProvider provider)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings { PropertyNameProvider = provider },
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
