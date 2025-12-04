using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class SerializerTests
{
    private static RefitGeneratorSettings CreateTestSettings() => new()
    {
        OpenApiPath = "test.json",
        Namespace = "TestNamespace",
        GenerateContracts = true,
        GenerateClients = true
    };

    [Test]
    public void Can_Serialize_RefitGeneratorSettings()
    {
        var settings = CreateTestSettings();
        Serializer
            .Serialize(settings)
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Can_Deserialize_RefitGeneratorSettingsWithoutNameGenerators()
    {
        var settings = CreateTestSettings();
        var json = Serializer.Serialize(settings);
        Serializer
            .Deserialize<RefitGeneratorSettings>(json)
            .Should()
            .BeEquivalentTo(settings, options => options
                .Excluding(s => s.ParameterNameGenerator)
                .Excluding(s => s.CodeGeneratorSettings!.PropertyNameGenerator));
    }

    [Test]
    public void Deserialize_Is_Case_Insensitive()
    {
        var settings = CreateTestSettings();
        var json = Serializer.Serialize(settings);
        foreach (var property in typeof(RefitGeneratorSettings).GetProperties())
        {
            var jsonProperty = "\"" + property.Name + "\"";
            json = json.Replace(
                jsonProperty,
                jsonProperty.ToUpperInvariant());
        }

        Serializer
            .Deserialize<RefitGeneratorSettings>(json)
            .Should()
            .BeEquivalentTo(settings, options => options
                .Excluding(s => s.ParameterNameGenerator)
                .Excluding(s => s.CodeGeneratorSettings!.PropertyNameGenerator));
    }

    [Test]
    public void Deserialize_With_Comments()
    {
        var settings = CreateTestSettings();
        var json = Serializer.Serialize(settings);
        json = "// Comment\n" + json;

        Serializer
            .Deserialize<RefitGeneratorSettings>(json)
            .Should()
            .BeEquivalentTo(settings, options => options
                .Excluding(s => s.ParameterNameGenerator)
                .Excluding(s => s.CodeGeneratorSettings!.PropertyNameGenerator));
    }
}
