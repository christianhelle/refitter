using FluentAssertions;
using Refitter.Core;
using Xunit;

namespace Refitter.Tests;

public class RefitInterfaceImportTests
{
    [Fact]
    public void Should_Contain_SystemThreading()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = true };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().Contain("System.Threading");
    }

    [Fact]
    public void Should_NotContain_SystemThreading()
    {
        var settings = new RefitGeneratorSettings { UseCancellationTokens = false };
        var refitInterfaceImport = RefitInterfaceImports.GetImportedNamespaces(settings);
        refitInterfaceImport.Should().NotContain("System.Threading");
    }
}
