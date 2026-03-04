using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class ApizrGeneratorWithPollyTests
{
    private readonly RefitGeneratorSettings _extendedSettings = new()
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
        },
        ApizrSettings = new ApizrSettings
        {
            WithRequestOptions = true,
            WithRegistrationHelper = true,
            WithCacheProvider = CacheProviderType.InMemory,
            WithPriority = true,
            WithMediation = true,
            WithMappingProvider = MappingProviderType.AutoMapper,
            WithFileTransfer = true
        }
    };

    [Test]
    public void Can_Generate_Extended_Registration_For_Single_Interface()
    {
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
            [
                "IPetApi"
            ],
            "Pet");

        code.Should().Contain("AddApizrManagerFor<IPetApi>(optionsBuilder)");
    }

    [Test]
    public void Can_Generate_Extended_Registration_For_Multiple_Interfaces()
    {
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
            [
                "IPetApi",
                "IStoreApi"
            ],
            "PetStore");

        code.Should().Contain("AddManagerFor<IPetApi>()");
        code.Should().Contain("AddManagerFor<IStoreApi>()");
    }

    [Test]
    public void Can_Generate_With_HttpMessageHandlers()
    {
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("WithDelegatingHandler<AuthorizationMessageHandler>()");
        code.Should().Contain("WithDelegatingHandler<DiagnosticMessageHandler>()");
    }

    [Test]
    public void Can_Generate_With_Polly()
    {
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
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
    public void Can_Generate_Without_Polly()
    {
        _extendedSettings.DependencyInjectionSettings!.TransientErrorHandler = TransientErrorHandler.None;
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
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
        _extendedSettings.DependencyInjectionSettings!.BaseUrl = null;
        string code = ApizrRegistrationGenerator.Generate(
            _extendedSettings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("> optionsBuilder)");
    }
}
