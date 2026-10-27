using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


[TestClass]
public class MediationAdapterTests
{
    [TestMethod]
    public void CanApply_Returns_True_When_Mediation_Enabled_With_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var adapter = new MediationAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [TestMethod]
    public void CanApply_Returns_False_When_Mediation_Disabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = false
            },
            DependencyInjectionSettings = new DependencyInjectionSettings()
        };

        var adapter = new MediationAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [TestMethod]
    public void CanApply_Returns_False_When_Mediation_Enabled_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithMediation = true
            }
        };

        var adapter = new MediationAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [TestMethod]
    public void Apply_Adds_Mediation_To_Options()
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

        result.Should().Contain("WithMediation()");
        result.Should().Contain("using MediatR;");
        result.Should().Contain("Apizr.Integrations.MediatR");
    }
}
