using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.DependencyInjection;


[TestClass]
public class DependencyInjectionGeneratorWithMicrosoftHttpResilienceTests
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
            TransientErrorHandler = TransientErrorHandler.HttpResilience
        }
    };

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public void Can_Generate_With_HttpResilience()
    {
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().Contain("AddStandardResilienceHandler");
    }

    [TestMethod]
    public void Can_Generate_Without_TransientErrorHandler()
    {
        settings.DependencyInjectionSettings!.TransientErrorHandler = TransientErrorHandler.None;
        string code = DependencyInjectionGenerator.Generate(
            settings,
            new[]
            {
                "IPetApi",
                "IStoreApi"
            });

        code.Should().NotContain("using HttpResilience");
        code.Should().NotContain("Backoff.DecorrelatedJitterBackoffV2");
        code.Should().NotContain("AddPolicyHandler");
        code.Should().NotContain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [TestMethod]
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
