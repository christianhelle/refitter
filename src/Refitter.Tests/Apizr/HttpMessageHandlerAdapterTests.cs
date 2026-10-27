using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


[TestClass]
public class HttpMessageHandlerAdapterTests
{
    [TestMethod]
    public void CanApply_Returns_True_When_Handlers_Present()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                HttpMessageHandlers = ["AuthHandler"]
            }
        };

        var adapter = new HttpMessageHandlerAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [TestMethod]
    public void CanApply_Returns_False_When_No_Handlers()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var adapter = new HttpMessageHandlerAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [TestMethod]
    public void CanApply_Returns_False_When_No_DI_Settings()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings { WithRegistrationHelper = true }
        };

        var adapter = new HttpMessageHandlerAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [TestMethod]
    public void Apply_Adds_DelegatingHandlers()
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

        result.Should().Contain("WithDelegatingHandler<AuthHandler>()");
        result.Should().Contain("WithDelegatingHandler<LoggingHandler>()");
    }
}
