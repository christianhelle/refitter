using AwesomeAssertions;
using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NSwag;
using NSwag.CodeGeneration;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class CSharpClientGeneratorFactoryNameGeneratorTests
{
    private sealed class StubParameterNameGenerator : IParameterNameGenerator
    {
        public string Generate(OpenApiParameter parameter, IEnumerable<OpenApiParameter> allParameters) =>
            "stubbed" + parameter.Name;
    }

    private sealed class StubPropertyNameGenerator : IPropertyNameGenerator
    {
        public string Generate(JsonSchemaProperty property) => "Stubbed" + property.Name;
    }

    private static Task<OpenApiDocument> CreateDocumentAsync() =>
        OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TestModel": {
                    "type": "object",
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """);

    [TestMethod]
    public async Task Create_WithCustomParameterNameGenerator_UsesIt()
    {
        var document = await CreateDocumentAsync();
        var parameterNameGenerator = new StubParameterNameGenerator();
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ParameterNameGenerator = parameterNameGenerator,
        };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator.Should().BeSameAs(parameterNameGenerator);
    }

    [TestMethod]
    public async Task Create_WithoutCustomParameterNameGenerator_KeepsDefault()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings { Namespace = "TestNamespace" };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator.Should().NotBeNull();
        generator.Settings.ParameterNameGenerator.Should().NotBeOfType<StubParameterNameGenerator>();
    }

    [TestMethod]
    public async Task Create_WithCustomPropertyNameGenerator_UsesIt()
    {
        var document = await CreateDocumentAsync();
        var propertyNameGenerator = new StubPropertyNameGenerator();
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                PropertyNameGenerator = propertyNameGenerator,
            },
        };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.CSharpGeneratorSettings.PropertyNameGenerator
            .Should().BeSameAs(propertyNameGenerator);
    }

    [TestMethod]
    public async Task Create_WithPreserveOriginalPropertyNamingPolicy_UsesPreservingGenerator()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal,
        };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.CSharpGeneratorSettings.PropertyNameGenerator
            .Should().BeOfType<UniquePropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<PreserveOriginalPropertyNameGenerator>();
    }

    [TestMethod]
    public async Task Create_WithDefaultPropertyNamingPolicy_UsesUniqueCSharpGenerator()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings { Namespace = "TestNamespace" };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.CSharpGeneratorSettings.PropertyNameGenerator
            .Should().BeOfType<UniquePropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<CustomCSharpPropertyNameGenerator>();
    }
}
