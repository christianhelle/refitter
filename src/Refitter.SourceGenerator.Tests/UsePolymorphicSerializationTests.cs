using System.Text.Json.Serialization;
using AwesomeAssertions;
using Refitter.Tests.UsePolymorphicSerialization;

namespace Refitter.SourceGenerators.Tests;


[TestClass]
public class UsePolymorphicSerializationTests
{
    [TestMethod]
    public void Should_Generate_JsonPolymorphicAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonPolymorphicAttribute), false)
            .Should()
            .HaveCount(1);

    [TestMethod]
    public void Should_Generate_JsonDerivedTypeAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .Should()
            .HaveCount(5);
}
