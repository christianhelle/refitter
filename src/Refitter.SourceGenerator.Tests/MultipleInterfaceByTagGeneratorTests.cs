using AwesomeAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.ByTag;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class MultipleInterfaceByTagGeneratorTests
{
    private const string ExpectedNamespace = "Refitter.Tests.AdditionalFiles.ByTag";

    [TestMethod]
    public void Should_Generate_IPetApi_Interface() =>
        typeof(IPetApi).Namespace.Should().Be(ExpectedNamespace);

    [TestMethod]
    public void Should_Generate_IUserApi_Interface() =>
        typeof(IUserApi).Namespace.Should().Be(ExpectedNamespace);

    [TestMethod]
    public void Should_Generate_IStoreApi_Interface() =>
        typeof(IStoreApi).Namespace.Should().Be(ExpectedNamespace);

    [TestMethod]
    public void Can_Resolve_IPetApi_Refit_Interface()
    {
        var hasRefitAttributes = typeof(IPetApi)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }

    [TestMethod]
    public void Can_Resolve_IUserApi_Refit_Interface()
    {
        var hasRefitAttributes = typeof(IUserApi)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }

    [TestMethod]
    public void Can_Resolve_IStoreApi_Refit_Interface()
    {
        var hasRefitAttributes = typeof(IStoreApi)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue("interface should have at least one Refit HTTP method attribute");
    }
}
