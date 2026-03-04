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

    [Test]
    public void Can_Deserialize_CodeGeneratorSettings_With_IntegerType_String_Int64()
    {
        const string json = """{"integerType": "Int64"}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings.IntegerType.Should().Be(IntegerType.Int64);
    }

    [Test]
    public void Can_Deserialize_CodeGeneratorSettings_With_IntegerType_String_Int32()
    {
        const string json = """{"integerType": "Int32"}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings.IntegerType.Should().Be(IntegerType.Int32);
    }

    [Test]
    public void Can_Deserialize_CodeGeneratorSettings_With_IntegerType_Numeric_Value()
    {
        const string json = """{"integerType": 1}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings.IntegerType.Should().Be(IntegerType.Int64);
    }

    [Test]
    public void Can_Serialize_CodeGeneratorSettings_With_IntegerType()
    {
        var settings = new CodeGeneratorSettings { IntegerType = IntegerType.Int64 };
        var json = Serializer.Serialize(settings);
        json.Should().Contain("\"integerType\": \"Int64\"");
    }
}
