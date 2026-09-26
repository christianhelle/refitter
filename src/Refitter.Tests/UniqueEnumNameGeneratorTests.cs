using AwesomeAssertions;
using NJsonSchema;
using Refitter.Core;

namespace Refitter.Tests;

public class UniqueEnumNameGeneratorTests
{
    [Test]
    public void Suffixes_Names_That_Collide_After_Sanitizing()
    {
        var schema = CreateStringEnum("a", "A", "b", "a-");

        var names = GenerateAll(new UniqueEnumNameGenerator(), schema);

        names.Should().Equal("A", "A2", "B", "A3");
    }

    [Test]
    public void Keeps_Names_That_Do_Not_Collide()
    {
        var schema = CreateStringEnum("available", "pending", "sold");

        var names = GenerateAll(new UniqueEnumNameGenerator(), schema);

        names.Should().Equal("Available", "Pending", "Sold");
    }

    [Test]
    public void Skips_Suffixes_Already_Used_By_Other_Values()
    {
        var schema = CreateStringEnum("a", "A2", "A");

        var names = GenerateAll(new UniqueEnumNameGenerator(), schema);

        names.Should().Equal("A", "A2", "A3");
    }

    [Test]
    public void Uses_Enumeration_Names_And_Integer_Value_Names()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Integer };
        schema.Enumeration.Add(1);
        schema.Enumeration.Add(2);
        schema.Enumeration.Add(3);
        schema.EnumerationNames.Add("One");
        schema.EnumerationNames.Add("one");

        var names = GenerateAll(new UniqueEnumNameGenerator(), schema);

        names.Should().Equal("One", "One2", "_3");
    }

    [Test]
    public void Ignores_Null_Values_Like_NJsonSchema()
    {
        var schema = CreateStringEnum("a", null, "A");

        var generator = new UniqueEnumNameGenerator();

        generator.Generate(0, "a", "a", schema).Should().Be("A");
        generator.Generate(2, "A", "A", schema).Should().Be("A2");
    }

    [Test]
    public void Returns_The_Same_Names_When_Enumerated_Again()
    {
        var schema = CreateStringEnum("a", "A");
        var generator = new UniqueEnumNameGenerator();

        GenerateAll(generator, schema).Should().Equal("A", "A2");
        GenerateAll(generator, schema).Should().Equal("A", "A2");
    }

    private static JsonSchema CreateStringEnum(params string?[] values)
    {
        var schema = new JsonSchema { Type = JsonObjectType.String };
        foreach (var value in values)
        {
            schema.Enumeration.Add(value);
        }

        return schema;
    }

    private static List<string> GenerateAll(UniqueEnumNameGenerator generator, JsonSchema schema) =>
        schema.Enumeration
            .Select((value, index) => (value, index))
            .Where(x => x.value is not null)
            .Select(x => generator.Generate(
                x.index,
                schema.EnumerationNames.Count > x.index ? schema.EnumerationNames[x.index] : x.value!.ToString(),
                x.value,
                schema))
            .ToList();
}
