using FluentAssertions;
using Refitter.Core;
using Refitter.Core.Settings;
using TUnit.Core;

namespace Refitter.Tests;

public class ApizrRegistrationGeneratorTests
{
    [Test]
    public void Generate_Returns_Empty_String_When_No_Interfaces()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, [], "Test API");

        result.Should().BeEmpty();
    }

    [Test]
    public void Generate_Returns_Empty_String_When_Registration_Helper_Disabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = false }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().BeEmpty();
    }

    [Test]
    public void Generate_Creates_Static_Builder_Without_DependencyInjection()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("ApizrRegistration");
        result.Should().Contain("IApizrManager<ITestApi>");
        result.Should().NotContain("IServiceCollection");
    }

    [Test]
    public void Generate_Creates_DependencyInjection_Extension_With_Settings()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("IServiceCollection");
        result.Should().Contain("IServiceCollectionExtensions");
    }

    [Test]
    public void Generate_Creates_Registry_For_Multiple_Interfaces()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(
            settings,
            ["ITestApi", "IAnotherApi"],
            "Test API");

        result.Should().Contain("AddApizr");
        result.Should().Contain("AddManagerFor<ITestApi>");
        result.Should().Contain("AddManagerFor<IAnotherApi>");
        result.Should().Contain("IApizrExtendedCommonOptionsBuilder");
    }

    [Test]
    public void Generate_Includes_BaseUrl_When_Configured()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://api.example.com"
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithBaseAddress(\"https://api.example.com\"");
        result.Should().Contain("ApizrDuplicateStrategy.Ignore");
    }

    [Test]
    public void Generate_Includes_Polly_Configuration()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.Polly,
                MaxRetryCount = 3,
                FirstBackoffRetryInSeconds = 0.5
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("Polly");
        result.Should().Contain("using Polly.Extensions.Http;");
    }

    [Test]
    public void Generate_Includes_HttpResilience_Configuration()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.HttpResilience,
                MaxRetryCount = 5,
                FirstBackoffRetryInSeconds = 1.0
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("Microsoft.Extensions.Http.Resilience");
        result.Should().Contain("using Microsoft.Extensions.Http.Resilience;");
    }

    [Test]
    public void Generate_Includes_HttpMessageHandlers()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                HttpMessageHandlers = ["AuthHandler", "LoggingHandler"]
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithDelegatingHandler<AuthHandler>");
        result.Should().Contain("WithDelegatingHandler<LoggingHandler>");
    }

    [Test]
    public void Generate_Includes_Akavache_Cache_Provider()
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

        result.Should().Contain("Akavache");
        result.Should().Contain("WithAkavacheCacheHandler");
        result.Should().Contain("Apizr.Integrations.Akavache");
    }

    [Test]
    public void Generate_Includes_MonkeyCache_Cache_Provider()
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

        result.Should().Contain("MonkeyCache");
        result.Should().Contain("WithCacheHandler(new MonkeyCacheHandler(Barrel.Current))");
        result.Should().Contain("Apizr.Integrations.MonkeyCache");
    }

    [Test]
    public void Generate_Includes_InMemory_Cache_Provider()
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

        result.Should().Contain("WithInMemoryCacheHandler");
        result.Should().Contain("Apizr.Extensions.Microsoft.Caching");
    }

    [Test]
    public void Generate_Includes_DistributedCache_String_Provider()
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

        result.Should().Contain("WithDistributedCacheHandler<string>");
    }

    [Test]
    public void Generate_Includes_DistributedCache_ByteArray_Provider()
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

        result.Should().Contain("WithDistributedCacheHandler<byte[]>");
    }

    [Test]
    public void Generate_Includes_AutoMapper_Mapping_Provider()
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

        result.Should().Contain("AutoMapper");
        result.Should().Contain("WithAutoMapperMappingHandler");
        result.Should().Contain("Apizr.Integrations.AutoMapper");
    }

    [Test]
    public void Generate_Includes_Mapster_Mapping_Provider()
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

        result.Should().Contain("Mapster");
        result.Should().Contain("WithMapsterMappingHandler");
        result.Should().Contain("Apizr.Integrations.Mapster");
    }

    [Test]
    public void Generate_Includes_Priority_Support()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithPriority = true
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithPriority");
        result.Should().Contain("Apizr.Integrations.Fusillade");
    }

    [Test]
    public void Generate_Includes_Mediation_Support()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("MediatR");
        result.Should().Contain("WithMediation");
        result.Should().Contain("Apizr.Integrations.MediatR");
    }

    [Test]
    public void Generate_Includes_FileTransfer_With_Mediation()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithFileTransfer = true,
                WithMediation = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("WithFileTransferMediation");
        result.Should().Contain("Apizr.Integrations.FileTransfer.MediatR");
    }

    [Test]
    public void Generate_Includes_FileTransfer_Without_Mediation()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithFileTransfer = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("Apizr.Extensions.Microsoft.FileTransfer");
    }

    [Test]
    public void Generate_Uses_Custom_Extension_Method_Name()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                ExtensionMethodName = "RegisterMyCustomApi"
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("RegisterMyCustomApi");
    }

    [Test]
    public void Generate_Includes_XmlDoc_Comments_When_Enabled()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            GenerateXmlDocCodeComments = true,
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("/// <summary>");
        result.Should().Contain("/// <param name=\"optionsBuilder\">");
        result.Should().Contain("/// <returns>");
    }

    [Test]
    public void Generate_Error_Message_For_Extended_Features_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("/!\\ ERROR");
        result.Should().Contain("DependencyInjectionSettings");
    }

    [Test]
    public void Generate_Sanitizes_Title_For_Method_Name()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "My-Test API");

        result.Should().Contain("ConfigureMyTestAPIApizrManager");
    }
}
