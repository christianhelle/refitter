using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.OpenApi34;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class OpenApi34GeneratorTests
{
    [Test]
    public void Should_Generate_OpenApi_34_Interface() =>
        typeof(ISwaggerPetstoreOpenApi34)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.OpenApi34");

    [Test]
    public void Can_Resolve_OpenApi_34_Refit_Interface()
    {
        var hasRefitAttributes = typeof(ISwaggerPetstoreOpenApi34)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);

        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
