using AwesomeAssertions;
using Refitter.Tests.UseJsonInheritanceConverter;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class UseJsonInheritanceConverterTests
{
    [TestMethod]
    public void Should_Generate_JsonInheritanceConverter_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonInheritanceConverterAttribute), false)
            .Should()
            .HaveCount(1);

    [TestMethod]
    public void Should_Generate_JsonInheritanceAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonInheritanceAttribute), false)
            .Should()
            .HaveCount(5);
}
