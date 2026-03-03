using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.OptionalParameters;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class OperationalParametersGeneratorTest
{
    [Test]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstoreWithOptionalParameters)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.OptionalParameters");

    [Test]
    public void Can_Resolve_Refit_Interface()
    {
        var hasRefitAttributes = typeof(ISwaggerPetstoreWithOptionalParameters)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
