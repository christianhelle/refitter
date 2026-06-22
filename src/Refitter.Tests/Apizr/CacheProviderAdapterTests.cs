using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


public class CacheProviderAdapterTests
{
    [Test]
    public void CanApply_Returns_True_When_CacheProvider_Is_Set()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.Akavache
            }
        };

        var adapter = new CacheProviderAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_False_When_CacheProvider_Is_None()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.None
            }
        };

        var adapter = new CacheProviderAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void Apply_Adds_Akavache_To_Options()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.Akavache
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithAkavacheCacheHandler()");
        result.Should().Contain("using Akavache;");
        result.Should().Contain("Apizr.Integrations.Akavache");
    }

    [Test]
    public void Apply_Adds_MonkeyCache_To_Options()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.MonkeyCache
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithCacheHandler(new MonkeyCacheHandler(Barrel.Current))");
        result.Should().Contain("using MonkeyCache;");
        result.Should().Contain("Apizr.Integrations.MonkeyCache");
    }

    [Test]
    public void Apply_Adds_InMemory_Only_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithInMemoryCacheHandler()");
    }

    [Test]
    public void Apply_Omits_InMemory_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().NotContain("WithInMemoryCacheHandler()");
    }

    [Test]
    public void Apply_Adds_DistributedAsString_Only_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsString
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithDistributedCacheHandler<string>()");
    }

    [Test]
    public void Apply_Omits_DistributedAsString_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsString
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().NotContain("WithDistributedCacheHandler<string>()");
    }

    [Test]
    public void Apply_Adds_DistributedAsByteArray_Only_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsByteArray
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithDistributedCacheHandler<byte[]>()");
    }

    [Test]
    public void Apply_Omits_DistributedAsByteArray_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.DistributedAsByteArray
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().NotContain("WithDistributedCacheHandler<byte[]>()");
    }
}
