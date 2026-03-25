using System.Text.Json.Serialization;
using FluentAssertions;
using Refitter.Tests.AdditionalFiles.PropertyNamingPolicy;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class PropertyNamingPolicyTests
{
    [Test]
    public void Should_Generate_Preserved_Property_Name_From_Refitter_File()
    {
        var property = typeof(PaymentResponse).GetProperty("payMethod_SumBank");
        property.Should().NotBeNull();
    }

    [Test]
    public void Should_Keep_JsonPropertyName_Attribute_When_Preserving_Property_Names()
    {
        var property = typeof(PaymentResponse).GetProperty("payMethod_SumBank");
        property.Should().NotBeNull();

        var attribute = property!
            .GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<JsonPropertyNameAttribute>()
            .Subject;

        attribute.Name.Should().Be("payMethod_SumBank");
    }
}
