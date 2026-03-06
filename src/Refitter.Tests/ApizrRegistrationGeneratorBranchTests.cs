using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class ApizrRegistrationGeneratorBranchTests
{
    [Test]
    public void Can_Generate_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithRequestOptions = false
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithDelegatingHandler");
        code.Should().Contain("AddApizrManagerFor<IPetApi>");
    }

    [Test]
    public void Can_Generate_Polly_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithRequestOptions = false
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithDelegatingHandler");
        code.Should().Contain("using Polly");
        code.Should().Contain("AddPolicyHandler");
        code.Should().Contain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Test]
    public void Can_Generate_HttpResilience_With_Empty_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.HttpResilience,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithRequestOptions = false
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithDelegatingHandler");
        code.Should().Contain("using Microsoft.Extensions.Http.Resilience");
        code.Should().Contain("AddStandardResilienceHandler");
    }

    [Test]
    public void Can_Generate_Without_BaseUrl()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = null,
                HttpMessageHandlers = new[] { "AuthorizationMessageHandler" },
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithRequestOptions = false
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithBaseAddress");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_Akavache()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.Akavache
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using Akavache");
        code.Should().Contain("WithAkavacheCacheHandler()");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_MonkeyCache()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.MonkeyCache
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using MonkeyCache");
        code.Should().Contain("WithCacheHandler(new MonkeyCacheHandler(Barrel.Current))");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_InMemory()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithInMemoryCacheHandler()");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_DistributedAsString()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsString
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithDistributedCacheHandler<string>()");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_DistributedAsByteArray()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsByteArray
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithDistributedCacheHandler<byte[]>()");
    }

    [Test]
    public void Can_Generate_With_MappingProvider_AutoMapper_WithDI()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.AutoMapper
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using AutoMapper");
        code.Should().Contain("WithAutoMapperMappingHandler()");
        code.Should().NotContain("new MapperConfiguration");
    }

    [Test]
    public void Can_Generate_With_MappingProvider_AutoMapper_WithoutDI()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.AutoMapper
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using AutoMapper");
        code.Should().Contain("WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))");
    }

    [Test]
    public void Can_Generate_With_MappingProvider_Mapster_WithDI()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.Mapster
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using Mapster");
        code.Should().Contain("using MapsterMapper");
        code.Should().Contain("WithMapsterMappingHandler()");
        code.Should().NotContain("new Mapper()");
    }

    [Test]
    public void Can_Generate_With_MappingProvider_Mapster_WithoutDI()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMappingProvider = MappingProviderType.Mapster
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using Mapster");
        code.Should().NotContain("using MapsterMapper");
        code.Should().Contain("WithMapsterMappingHandler(new Mapper())");
    }

    [Test]
    public void Can_Generate_With_Priority()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithPriority = true
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithPriority()");
    }

    [Test]
    public void Can_Generate_With_Mediation_WithDI()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = Array.Empty<string>(),
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("using MediatR");
        code.Should().Contain("WithMediation()");
    }

    [Test]
    public void Can_Generate_With_Mediation_WithoutDI_ShouldNotGenerate()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithMediation()");
    }

    [Test]
    public void Can_Generate_With_TransientErrorHandler_None_And_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = new[]
                {
                    "AuthorizationMessageHandler",
                    "DiagnosticMessageHandler"
                },
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithDelegatingHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("WithDelegatingHandler<DiagnosticMessageHandler>()");
        code.Should().NotContain("using Polly");
        code.Should().NotContain("using Microsoft.Extensions.Http.Resilience");
        code.Should().NotContain("AddPolicyHandler");
        code.Should().NotContain("AddStandardResilienceHandler");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_InMemory_WithoutDI_ShouldNotGenerate()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithInMemoryCacheHandler()");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_DistributedAsString_WithoutDI_ShouldNotGenerate()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsString
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithDistributedCacheHandler<string>()");
    }

    [Test]
    public void Can_Generate_With_CacheProvider_DistributedAsByteArray_WithoutDI_ShouldNotGenerate()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = null,
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsByteArray
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().NotContain("WithDistributedCacheHandler<byte[]>()");
    }

    [Test]
    public void Can_Generate_Combined_Settings_All_Features()
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                HttpMessageHandlers = new[] { "AuthorizationMessageHandler" },
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            },
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithPriority = true,
                WithMediation = true
            }
        };

        string code = ApizrRegistrationGenerator.Generate(
            settings,
            new[] { "IPetApi" },
            "Pet");

        code.Should().Contain("WithBaseAddress");
        code.Should().Contain("WithDelegatingHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("using Polly");
        code.Should().Contain("AddPolicyHandler");
        code.Should().Contain("WithInMemoryCacheHandler()");
        code.Should().Contain("WithAutoMapperMappingHandler()");
        code.Should().Contain("WithPriority()");
        code.Should().Contain("WithMediation()");
    }
}
