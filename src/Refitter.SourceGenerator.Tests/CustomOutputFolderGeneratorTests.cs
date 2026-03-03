using FluentAssertions;
using Refit;
using Refitter.Tests.CustomGenerated;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class CustomOutputFolderGeneratorTests
{
    [Test]
    public void Should_Generate_Interface_Type() =>
        typeof(IApiInCustomGeneratedFolder)
            .Namespace
            .Should()
            .Be("Refitter.Tests.CustomGenerated");

    [Test]
    public void Can_Resolve_Refit_Interface()
    {
        var hasRefitAttributes = typeof(IApiInCustomGeneratedFolder)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
