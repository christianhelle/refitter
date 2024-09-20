using FluentAssertions;
using Refitter.Tests.UseJsonInheritanceConverter;
using Xunit;

namespace Refitter.SourceGenerators.Tests;

public class UseJsonInheritanceConverterTests
{
    [Theory]
    [InlineData(typeof(SomeComponent))]
    public void Should_Generate_JsonInheritanceConverter_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonInheritanceConverterAttribute), false)
            .Should()
            .HaveCount(1);

    [Theory]
    [InlineData(typeof(SomeComponent))]
    public void Should_Generate_JsonInheritanceAttribute_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonInheritanceAttribute), false)
            .Should()
            .HaveCount(5);
}
