using FluentAssertions;
using Refitter.Tests.UseJsonInheritanceConverter;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class UseJsonInheritanceConverterTests
{
    [Test]
    public void Should_Generate_JsonInheritanceConverter_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonInheritanceConverterAttribute), false)
            .Should()
            .HaveCount(1);

    [Test]
    public void Should_Generate_JsonInheritanceAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonInheritanceAttribute), false)
            .Should()
            .HaveCount(5);
}
