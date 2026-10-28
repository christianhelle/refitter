using AwesomeAssertions;
using Refit;
using Refitter.Tests.CustomGenerated;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class CustomOutputFolderGeneratorTests
{
    [TestMethod]
    public void Should_Generate_Interface_Type() =>
        typeof(IApiInCustomGeneratedFolder)
            .Namespace
            .Should()
            .Be("Refitter.Tests.CustomGenerated");

    [TestMethod]
    public void Can_Resolve_Refit_Interface()
    {
        var hasRefitAttributes = typeof(IApiInCustomGeneratedFolder)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
