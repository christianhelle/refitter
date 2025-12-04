using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitInterfaceImportTests
{
    [Test]
    public void Should_Contain_SystemThreading()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = true };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().Contain("System.Threading");
    }

    [Test]
    public void Should_NotContain_SystemThreading()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = false };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().NotContain("System.Threading");
    }

    [Test]
    public void Should_NotContain_Any_System_Excluded()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = true, ReturnIObservable = false, ExcludeNamespaces = new string[] { "^System[.].*" } };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().NotContain("System.Collections.Generic");
        refitInterfaceImport.Should().NotContain("System.Text.Json.Serialization");
        refitInterfaceImport.Should().NotContain("System.Threading");
        refitInterfaceImport.Should().NotContain("System.Threading.Tasks");
    }

    [Test]
    public void Should_NotContain_SystemThreading_Excluded()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = true, ReturnIObservable = false, ExcludeNamespaces = new string[] { "System.Threading$" } };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().NotContain("System.Threading");
        refitInterfaceImport.Should().Contain("System.Threading.Tasks");
    }

    [Test]
    public void Should_Contain_ApizrConfiguringRequest()
    {
        var settings = new RefitGeneratorSettings { ApizrSettings = new ApizrSettings { WithRequestOptions = true, WithRegistrationHelper = true } };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().Contain("Apizr.Configuring.Request");
    }

    [Test]
    public void Should_Contain_ApizrConfiguringRequest_And_NotContain_SystemThreading()
    {
        var settings = new RefitGeneratorSettings { ApizrSettings = new ApizrSettings { WithRequestOptions = true, WithRegistrationHelper = true }, UseCancellationTokens = true };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().Contain("Apizr.Configuring.Request");
        refitInterfaceImport.Should().NotContain("System.Threading");
    }
}
