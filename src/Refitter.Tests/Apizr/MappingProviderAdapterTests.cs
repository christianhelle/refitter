using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


public class MappingProviderAdapterTests
{
    [Test]
    public void CanApply_Returns_True_When_MappingProvider_Is_Set()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.AutoMapper
            }
        };

        var adapter = new MappingProviderAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_False_When_MappingProvider_Is_None()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.None
            }
        };

        var adapter = new MappingProviderAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void Apply_Adds_AutoMapper_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.AutoMapper
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithAutoMapperMappingHandler()");
        result.Should().NotContain("new MapperConfiguration");
        result.Should().Contain("using AutoMapper;");
        result.Should().Contain("Apizr.Integrations.AutoMapper");
    }

    [Test]
    public void Apply_Adds_AutoMapper_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.AutoMapper
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))");
        result.Should().Contain("using AutoMapper;");
    }

    [Test]
    public void Apply_Adds_Mapster_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.Mapster
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithMapsterMappingHandler()");
        result.Should().NotContain("new Mapper()");
        result.Should().Contain("using Mapster;");
        result.Should().Contain("using MapsterMapper;");
        result.Should().Contain("Apizr.Integrations.Mapster");
    }

    [Test]
    public void Apply_Adds_Mapster_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.Mapster
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithMapsterMappingHandler(new Mapper())");
        result.Should().Contain("using Mapster;");
        result.Should().NotContain("using MapsterMapper;");
    }
}
