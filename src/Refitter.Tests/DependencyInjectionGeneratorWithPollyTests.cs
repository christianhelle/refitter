using FluentAssertions;

using Refitter.Core;

using Xunit;

namespace Refitter.Tests;

public class DependencyInjectionGeneratorWithPollyTests
{
    private readonly RefitGeneratorSettings settings = new()
    {
        DependencyInjectionSettings = new DependencyInjectionSettings
        {
            BaseUrl = "https://petstore3.swagger.io/api/v3",
            HttpMessageHandlers = new[]
            {
                "AuthorizationMessageHandler",
                "DiagnosticMessageHandler"
            },
            HandleTransientErrors = TransientErrorHandler.Polly,
            PollyMaxRetryCount = 3,
            FirstBackoffRetryInSeconds = 0.5,
        }
    };

    [Fact]
    public void Can_Generate_For_Multiple_Interfaces()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("AddRefitClient<IPetApi>()");
        code.Should().Contain("AddRefitClient<IStoreApi>()");
    }

    [Fact]
    public void Can_Generate_With_HttpMessageHandlers()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("AddHttpMessageHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("AddHttpMessageHandler<DiagnosticMessageHandler>()");
    }

    [Fact]
    public void Can_Generate_With_Polly()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("using Polly");
        code.Should().Contain("AddPolicyHandler");
        code.Should().Contain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Fact]
    public void Can_Generate_Without_Polly()
    {
        settings.DependencyInjectionSettings!.HandleTransientErrors = TransientErrorHandler.None;
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().NotContain("using Polly");
        code.Should().NotContain("Backoff.DecorrelatedJitterBackoffV2");
        code.Should().NotContain("AddPolicyHandler");
        code.Should().NotContain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Fact]
    public void Can_Generate_Without_BaseUrl()
    {
        settings.DependencyInjectionSettings!.BaseUrl = null;
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("Uri baseUrl");
        code.Should().Contain(".ConfigureHttpClient(c => c.BaseAddress = baseUrl)");
    }
}