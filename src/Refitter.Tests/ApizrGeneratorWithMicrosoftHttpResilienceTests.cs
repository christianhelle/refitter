using FluentAssertions;

using Refitter.Core;

using Xunit;

namespace Refitter.Tests;

public class ApizrGeneratorWithMicrosoftHttpResilienceTests
{
    private readonly RefitGeneratorSettings settings = new()
    {
        DependencyInjectionSettings = new DependencyInjectionSettings
        {
            BaseUrl = "https://petstore3.swagger.io/api/v3",
            HttpMessageHandlers =
            [
                "AuthorizationMessageHandler",
                "DiagnosticMessageHandler"
            ],
            TransientErrorHandler = TransientErrorHandler.HttpResilience
        },
        UseApizr = true
    };

    [Fact]
    public void Can_Generate_For_Single_Interface()
    {
        string code = ApizrRegistrationGenerator.Generate(
            settings,
            [
                "IPetApi"
            ],
            "Pet");

        code.Should().Contain("AddApizrManagerFor<IPetApi>(optionsBuilder)");
    }

    [Fact]
    public void Can_Generate_For_Multiple_Interfaces()
    {
        string code = ApizrRegistrationGenerator.Generate(
            settings,
            [
                "IPetApi",
                "IStoreApi"
            ],
            "PetStore");

        code.Should().Contain("AddManagerFor<IPetApi>()");
        code.Should().Contain("AddManagerFor<IStoreApi>()");
    }

    [Fact]
    public void Can_Generate_With_HttpMessageHandlers()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            [
                "IPetApi",
                "IStoreApi"
            ]);

        code.Should().Contain("AddHttpMessageHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("AddHttpMessageHandler<DiagnosticMessageHandler>()");
    }

    [Fact]
    public void Can_Generate_With_HttpResilience()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            [
                "IPetApi",
                "IStoreApi"
            ]);
    }

    [Fact]
    public void Can_Generate_Without_TransientErrorHandler()
    {
        settings.DependencyInjectionSettings!.TransientErrorHandler = TransientErrorHandler.None;
        string code = DependencyInjectionGenerator.Generate(
            settings,
            [
                "IPetApi",
                "IStoreApi"
            ]);

        code.Should().NotContain("using HttpResilience");
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
            [
                "IPetApi",
                "IStoreApi"
            ]);

        code.Should().Contain("Uri baseUrl");
        code.Should().Contain(".ConfigureHttpClient(c => c.BaseAddress = baseUrl)");
    }
}