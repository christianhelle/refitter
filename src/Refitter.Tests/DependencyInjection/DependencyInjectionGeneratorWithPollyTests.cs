using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.DependencyInjection;


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
            TransientErrorHandler = TransientErrorHandler.Polly,
            MaxRetryCount = 3,
            FirstBackoffRetryInSeconds = 0.5,
        }
    };

    [Test]
    public void Can_Generate_For_Multiple_Interfaces()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("AddRefitClient<IPetApi>(settings)");
        code.Should().Contain("AddRefitClient<IStoreApi>(settings)");
    }

    [Test]
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

    [Test]
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

    [Test]
    public void Can_Generate_With_Windows_Authentication_And_Polly()
    {
        settings.DependencyInjectionSettings!.UseWindowsAuthentication = true;

        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("using System.Net.Http");
        code.Should().Contain(".ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true })");
        code.Should().Contain(".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"https://petstore3.swagger.io/api/v3\"))");
        code.IndexOf(".ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true })")
            .Should().BeLessThan(code.IndexOf(".ConfigureHttpClient(c => c.BaseAddress = new Uri(\"https://petstore3.swagger.io/api/v3\"))"));
        code.IndexOf(".ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true })")
            .Should().BeLessThan(code.IndexOf(".AddHttpMessageHandler<AuthorizationMessageHandler>()"));
        code.IndexOf(".AddHttpMessageHandler<DiagnosticMessageHandler>()")
            .Should().BeLessThan(code.IndexOf(".AddPolicyHandler("));
    }

    [Test]
    public void Can_Generate_Without_Polly()
    {
        settings.DependencyInjectionSettings!.TransientErrorHandler = TransientErrorHandler.None;
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

    [Test]
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
