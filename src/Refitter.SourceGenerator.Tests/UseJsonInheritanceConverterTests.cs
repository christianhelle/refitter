using FluentAssertions;
using Refitter.Tests.UseJsonInheritanceConverter;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class UseJsonInheritanceConverterTests
{
    [Test]
    [Arguments(typeof(SomeComponent))]
    public void Should_Generate_JsonInheritanceConverter_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonInheritanceConverterAttribute), false)
            .Should()
            .HaveCount(1);

    [Test]
    [Arguments(typeof(SomeComponent))]
    public void Should_Generate_JsonInheritanceAttribute_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonInheritanceAttribute), false)
            .Should()
            .HaveCount(5);
}
