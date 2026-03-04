using System.Text.Json.Serialization;
using FluentAssertions;
using Refitter.Tests.UsePolymorphicSerialization;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class UsePolymorphicSerializationTests
{
    [Test]
    public void Should_Generate_JsonPolymorphicAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonPolymorphicAttribute), false)
            .Should()
            .HaveCount(1);

    [Test]
    public void Should_Generate_JsonDerivedTypeAttribute_Usage() =>
        typeof(SomeComponent)
            .GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .Should()
            .HaveCount(5);
}
