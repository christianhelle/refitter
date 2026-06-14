using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;

public class RetryHandlerAdapterTests
{
    [Test]
    public void CanApply_Returns_True_For_Polly()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.Polly
            }
        };

        var adapter = new RetryHandlerAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_True_For_HttpResilience()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.HttpResilience
            }
        };

        var adapter = new RetryHandlerAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_False_For_None()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                TransientErrorHandler = TransientErrorHandler.None
            }
        };

        var adapter = new RetryHandlerAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void CanApply_Returns_False_When_No_DI_Settings()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true }
        };

        var adapter = new RetryHandlerAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void Apply_Adds_Polly_Configuration()
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

        result.Should().Contain("using Polly;");
        result.Should().Contain("using Polly.Extensions.Http;");
        result.Should().Contain("AddPolicyHandler");
        result.Should().Contain("Backoff.DecorrelatedJitterBackoffV2");
        result.Should().Contain("TimeSpan.FromSeconds(0.5)");
        result.Should().Contain("3");
    }

    [Test]
    public void Apply_Adds_HttpResilience_Configuration()
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

        result.Should().Contain("using Microsoft.Extensions.Http.Resilience;");
        result.Should().Contain("AddStandardResilienceHandler");
        result.Should().Contain("MaxRetryAttempts = 5");
        result.Should().Contain("TimeSpan.FromSeconds(1)");
    }
}
