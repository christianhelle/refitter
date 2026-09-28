using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class ContractGeneratorFactoryNameGeneratorTests
{
    private sealed class StubParameterNameProvider : IParameterNameProvider
    {
        public string GetParameterName(ParameterNameContext context) => "provided" + context.Name;
    }

    private sealed class RecordingParameterNameProvider : IParameterNameProvider
    {
        public List<ParameterNameContext> Contexts { get; } = new();

        public string GetParameterName(ParameterNameContext context)
        {
            Contexts.Add(context);
            return context.Name;
        }
    }

    private sealed class StubPropertyNameProvider : IPropertyNameProvider
    {
        public string GetPropertyName(PropertyNameContext context) => "Provided" + context.Name;
    }

    private static Task<ApiDocument> CreateDocumentAsync() =>
        Task.FromResult(ApiDocumentLoader.Load("""
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
            """, null, isYaml: false));

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

        var generator = new ContractGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator
            .Should().BeOfType<ProviderOperationParameterNameGenerator>()
            .Which.Provider.Should().BeSameAs(parameterNameProvider);
    }

    [Test]
    public void Parameter_Name_Providers_Receive_The_Parameter_Source()
    {
        var provider = new RecordingParameterNameProvider();
        var generator = new ProviderOperationParameterNameGenerator(provider);
        ApiParameterKind[] kinds =
        [
            ApiParameterKind.Path,
            ApiParameterKind.Query,
            ApiParameterKind.Header,
            ApiParameterKind.Cookie,
            ApiParameterKind.Body,
            ApiParameterKind.FormData,
            ApiParameterKind.ModelBinding,
            ApiParameterKind.Undefined,
        ];
        var parameters = kinds.Select(kind => new ApiParameter { Name = kind.ToString(), Kind = kind, IsRequired = kind == ApiParameterKind.Path }).ToList();

        foreach (var parameter in parameters)
            generator.Generate(parameter, parameters);

        provider.Contexts.Select(c => c.Source).Should().Equal(
            ParameterSource.Path,
            ParameterSource.Query,
            ParameterSource.Header,
            ParameterSource.Cookie,
            ParameterSource.Body,
            ParameterSource.Form,
            ParameterSource.Other,
            ParameterSource.Other);
        provider.Contexts[0].IsRequired.Should().BeTrue();
        provider.Contexts[0].AllParameterNames.Should().HaveCount(kinds.Length);
    }

    [Test]
    public async Task Create_WithoutCustomParameterNameGenerator_KeepsDefault()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings { Namespace = "TestNamespace" };

        var generator = new ContractGeneratorFactory(settings, document).Create();

        generator.Settings.ParameterNameGenerator.Should().NotBeNull();
        generator.Settings.ParameterNameGenerator.Should().NotBeOfType<ProviderOperationParameterNameGenerator>();
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

        var generator = new ContractGeneratorFactory(settings, document).Create();

        generator.Settings.PropertyNameGenerator
            .Should().BeOfType<UniqueContractPropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<ProviderContractPropertyNameGenerator>()
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

        var generator = new ContractGeneratorFactory(settings, document).Create();

        generator.Settings.PropertyNameGenerator
            .Should().BeOfType<UniqueContractPropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<PreserveOriginalContractPropertyNameGenerator>();
    }

    [Test]
    public async Task Create_WithDefaultPropertyNamingPolicy_UsesUniqueCSharpGenerator()
    {
        var document = await CreateDocumentAsync();
        var settings = new RefitGeneratorSettings { Namespace = "TestNamespace" };

        var generator = new ContractGeneratorFactory(settings, document).Create();

        generator.Settings.PropertyNameGenerator
            .Should().BeOfType<UniqueContractPropertyNameGenerator>()
            .Which.Inner.Should().BeOfType<ContractPropertyNameGenerator>();
    }
}
