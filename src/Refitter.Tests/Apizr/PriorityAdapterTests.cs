using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;

public class PriorityAdapterTests
{
    [Test]
    public void CanApply_Returns_True_When_Priority_Enabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithPriority = true
            }
        };

        var adapter = new PriorityAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_False_When_Priority_Disabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithPriority = false
            }
        };

        var adapter = new PriorityAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void Apply_Adds_Priority_To_Options()
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

        result.Should().Contain("WithPriority()");
        result.Should().Contain("Apizr.Integrations.Fusillade");
    }
}
