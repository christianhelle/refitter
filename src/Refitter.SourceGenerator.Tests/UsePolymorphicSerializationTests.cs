using System.Text.Json.Serialization;
using FluentAssertions;
using Refitter.Tests.UsePolymorphicSerialization;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class UsePolymorphicSerializationTests
{
    [Test]
    [Arguments(typeof(SomeComponent))]
    public void Should_Generate_JsonPolymorphicAttribute_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonPolymorphicAttribute), false)
            .Should()
            .HaveCount(1);

    [Test]
    [Arguments(typeof(SomeComponent))]
    public void Should_Generate_JsonDerivedTypeAttribute_Usage(Type type) =>
        type
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .Should()
            .HaveCount(5);
}
