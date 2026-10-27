using AwesomeAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.OptionalParameters;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class OperationalParametersGeneratorTest
{
    [TestMethod]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstoreWithOptionalParameters)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.OptionalParameters");

    [TestMethod]
    public void Can_Resolve_Refit_Interface()
    {
        var hasRefitAttributes = typeof(ISwaggerPetstoreWithOptionalParameters)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
