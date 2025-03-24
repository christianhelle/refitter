using System.Reflection;
using Atc.Test;
using FluentAssertions;
using Refitter.Core;
using Xunit;

namespace Refitter.Tests;

public class SerializerTests
{
    [Theory, AutoNSubstituteData]
    public void Can_Serialize_RefitGeneratorSettings(
        RefitGeneratorSettings settings)
    {
        Serializer
            .Serialize(settings)
            .Should()
            .NotBeNullOrWhiteSpace();
    }

    [Theory, AutoNSubstituteData]
    public void Can_Deserialize_RefitGeneratorSettingsWithoutNameGenerators(
        RefitGeneratorSettings settings)
    {
        var json = Serializer.Serialize(settings);
        Serializer
            .Deserialize<RefitGeneratorSettings>(json)
            .Should()
            .BeEquivalentTo(settings, options => options
                .Excluding(settings => settings.ParameterNameGenerator)
                .Excluding(settings => settings.CodeGeneratorSettings!.PropertyNameGenerator));
    }

    [Theory, AutoNSubstituteData]
    public void Deserialize_Is_Case_Insensitive(
        RefitGeneratorSettings settings)
    {
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
                .Excluding(settings => settings.ParameterNameGenerator)
                .Excluding(settings => settings.CodeGeneratorSettings!.PropertyNameGenerator));
    }

    [Theory, AutoNSubstituteData]
    public void Deserialize_With_Comments(
        RefitGeneratorSettings settings)
    {
        var json = Serializer.Serialize(settings);
        json = "// Comment\n" + json;

        Serializer
            .Deserialize<RefitGeneratorSettings>(json)
            .Should()
            .BeEquivalentTo(settings, options => options
                .Excluding(settings => settings.ParameterNameGenerator)
                .Excluding(settings => settings.CodeGeneratorSettings!.PropertyNameGenerator));
    }
}
