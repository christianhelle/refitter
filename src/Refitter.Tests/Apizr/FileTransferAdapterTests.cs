using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;


public class FileTransferAdapterTests
{
    [Test]
    public void CanApply_Returns_True_When_FileTransfer_Enabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithFileTransfer = true
            }
        };

        var adapter = new FileTransferAdapter();
        adapter.CanApply(settings).Should().BeTrue();
    }

    [Test]
    public void CanApply_Returns_False_When_FileTransfer_Disabled()
    {
        var settings = new RefitGeneratorSettings
        {
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithFileTransfer = false
            }
        };

        var adapter = new FileTransferAdapter();
        adapter.CanApply(settings).Should().BeFalse();
    }

    [Test]
    public void Apply_Adds_FileTransferMediation_With_DI_And_Mediation()
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

        result.Should().Contain("WithFileTransferMediation()");
        result.Should().Contain("Apizr.Integrations.FileTransfer.MediatR");
    }

    [Test]
    public void Apply_Adds_FileTransfer_Package_With_DI_Without_Mediation()
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
        result.Should().NotContain("WithFileTransferMediation()");
    }

    [Test]
    public void Apply_Adds_FileTransfer_Package_Without_DI()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            ApizrSettings = new ApizrSettings
            {
                WithRegistrationHelper = true,
                WithFileTransfer = true
            }
        };

        var result = ApizrRegistrationGenerator.Generate(settings, ["ITestApi"], "Test API");

        result.Should().Contain("Apizr.Integrations.FileTransfer");
        result.Should().NotContain("WithFileTransferMediation()");
    }
}
