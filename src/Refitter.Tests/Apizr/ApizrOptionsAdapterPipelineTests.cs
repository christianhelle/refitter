using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


[Category("Unit")]
public class ApizrOptionsAdapterPipelineTests
{
    [Test]
    public void Adapters_Combine_Cache_And_Mapping()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.Akavache,
                WithMappingProvider = MappingProviderType.AutoMapper
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithAkavacheCacheHandler()");
        result.Should().Contain("WithAutoMapperMappingHandler()");
        result.Should().Contain("using Akavache;");
        result.Should().Contain("using AutoMapper;");
        result.Should().Contain("Apizr.Integrations.Akavache");
        result.Should().Contain("Apizr.Integrations.AutoMapper");
    }

    [Test]
    public void Adapters_Combine_Retry_And_Mediation()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("AddPolicyHandler");
        result.Should().Contain("WithMediation()");
        result.Should().Contain("using Polly;");
        result.Should().Contain("using MediatR;");
    }

    [Test]
    public void Pipeline_Order_Is_Correct()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithPriority = true,
                WithMediation = true,
                WithFileTransfer = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://api.example.com",
                HttpMessageHandlers = ["AuthHandler"],
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        // Base address
        result.Should().Contain("WithBaseAddress(\"https://api.example.com\"");
        // Message handlers
        result.Should().Contain("WithDelegatingHandler<AuthHandler>()");
        // Retry
        result.Should().Contain("AddPolicyHandler");
        // Cache
        result.Should().Contain("WithInMemoryCacheHandler()");
        // Mapping
        result.Should().Contain("WithAutoMapperMappingHandler()");
        // Priority
        result.Should().Contain("WithPriority()");
        // Mediation
        result.Should().Contain("WithMediation()");
        // File transfer
        result.Should().Contain("WithFileTransferMediation()");
        result.Should().Contain("Apizr.Integrations.FileTransfer.MediatR");
    }

    [Test]
    public void Pipeline_Produces_Compilable_Code_With_All_Features()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.Akavache,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithPriority = true,
                WithMediation = true,
                WithFileTransfer = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://api.example.com",
                HttpMessageHandlers = ["AuthHandler"],
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().NotContain("/!\\ ERROR");
        result.Should().Contain("WithBaseAddress");
        result.Should().Contain("WithDelegatingHandler<AuthHandler>()");
        result.Should().Contain("AddPolicyHandler");
        result.Should().Contain("WithAkavacheCacheHandler()");
        result.Should().Contain("WithAutoMapperMappingHandler()");
        result.Should().Contain("WithPriority()");
        result.Should().Contain("WithMediation()");
        result.Should().Contain("Apizr.Integrations.FileTransfer.MediatR");
    }

    [Test]
    public void Pipeline_Generates_Error_For_Extended_Features_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true,
                WithCacheProvider = CacheProviderType.InMemory
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("/!\\ ERROR");
        result.Should().Contain("DependencyInjectionSettings");
    }
}
