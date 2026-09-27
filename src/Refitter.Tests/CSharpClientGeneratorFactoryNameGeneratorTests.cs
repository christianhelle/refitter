using AwesomeAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class CSharpClientGeneratorFactoryNameGeneratorTests
{
    private sealed class StubParameterNameProvider : IParameterNameProvider
    {
        public string GetParameterName(ParameterNameContext context) => "provided" + context.Name;
    }

    private sealed class StubPropertyNameProvider : IPropertyNameProvider
    {
        public string GetPropertyName(PropertyNameContext context) => "Provided" + context.Name;
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

    [Test]
    public async Task Create_WithParameterNameProvider_UsesIt()
    {
        var document = await CreateDocumentAsync();
        var parameterNameProvider = new StubParameterNameProvider();
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ParameterNameProvider = parameterNameProvider,
        };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator
            .Should().BeOfType<ParameterNameProviderAdapter>()
            .Which.Provider.Should().BeSameAs(parameterNameProvider);
    }

    [Test]
    public async Task Create_WithoutCustomParameterNameGenerator_KeepsDefault()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings { Namespace = "TestNamespace" };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator.Should().NotBeNull();
        generator.Settings.ParameterNameGenerator.Should().NotBeOfType<ParameterNameProviderAdapter>();
    }

    [Test]
    public async Task Create_WithPropertyNameProvider_UsesIt()
    {
        var document = await CreateDocumentAsync();
        var propertyNameProvider = new StubPropertyNameProvider();
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                PropertyNameProvider = propertyNameProvider,
            },
        };

        var generator = new CSharpClientGeneratorFactory(settings, document).Create();

        generator.Settings.CSharpGeneratorSettings.PropertyNameGenerator
            .Should().BeOfType<UniquePropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<PropertyNameProviderAdapter>()
            .Which.Provider.Should().BeSameAs(propertyNameProvider);
    }

    [Test]
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

    [Test]
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
