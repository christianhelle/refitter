using AwesomeAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.SingeInterface;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class SingleInterfaceGeneratorTest
{
    [TestMethod]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstoreInterface)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.SingeInterface");

    [TestMethod]
    public void Can_Resolve_Refit_Interface()
    {
        var hasRefitAttributes = typeof(ISwaggerPetstoreInterface)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
