using FluentAssertions;
using Refit;
using Refitter.Tests.PropertyNamingPolicy;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class PropertyNamingPolicyGeneratorTests
{
    [Test]
    public void Should_Generate_Refit_Interface_For_Recursive_Property_Naming_Scenario()
    {
        typeof(IRecursivePropertyNamingApi)
            .Namespace
            .Should()
            .Be("Refitter.Tests.PropertyNamingPolicy");

        typeof(IRecursivePropertyNamingApi)
            .GetMethods()
            .SelectMany(method => method.GetCustomAttributes(inherit: false))
            .Any(attribute => attribute is HttpMethodAttribute)
            .Should()
            .BeTrue("interface should have at least one Refit HTTP method attribute");
    }

    [Test]
    public void Should_Preserve_Recursive_Property_Names()
    {
        var recursiveNodeType = typeof(RecursiveNode);

        recursiveNodeType.GetProperty("node_id").Should().NotBeNull();
        recursiveNodeType.GetProperty("class").Should().NotBeNull();
        recursiveNodeType.GetProperty("_1st_node").Should().NotBeNull();
        recursiveNodeType.GetProperty("child_count")!.PropertyType.Should().Be(typeof(long));
    }

    [Test]
    public void Should_Generate_Recursive_Properties_And_Use_Stubbed_Excluded_Type()
    {
        var recursiveNodeType = typeof(RecursiveNode);

        recursiveNodeType.GetProperty("next_node")!.PropertyType.Should().Be(typeof(RecursiveNode));
        recursiveNodeType.GetProperty("children")!.PropertyType.Should().Be(typeof(ICollection<RecursiveNode>));
        recursiveNodeType.GetProperty("named_nodes")!.PropertyType.Should().Be(typeof(IDictionary<string, RecursiveNode>));
        recursiveNodeType.GetProperty("external_node")!.PropertyType.Should().Be(typeof(RecursiveExternalNode));
    }
}
