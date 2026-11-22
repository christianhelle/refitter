using AwesomeAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class NonLiteralValueMutatorTests
{
    [TestMethod]
    [DataRow(JsonObjectType.Integer, "42", true)]
    [DataRow(JsonObjectType.Integer, "-1.5e3", true)]
    [DataRow(JsonObjectType.Integer, "0; evil", false)]
    [DataRow(JsonObjectType.Number, "NaN", false)]
    [DataRow(JsonObjectType.Boolean, "true", true)]
    [DataRow(JsonObjectType.Boolean, "FALSE", true)]
    [DataRow(JsonObjectType.Boolean, "true; evil", false)]
    [DataRow(JsonObjectType.Boolean | JsonObjectType.Integer, "7", true)]
    [DataRow(JsonObjectType.Boolean | JsonObjectType.Integer, "x", false)]
    [DataRow(JsonObjectType.String | JsonObjectType.Integer, "x", false)]
    [DataRow(JsonObjectType.String, "x\"; evil", true)]
    [DataRow(JsonObjectType.None, "x", true)]
    public void Keeps_Only_Literal_String_Defaults(JsonObjectType type, string defaultValue, bool kept)
    {
        var schema = Mutate(new JsonSchema { Type = type, Default = defaultValue });

        if (kept)
            schema.Default.Should().Be(defaultValue);
        else
            schema.Default.Should().BeNull();
    }

    [TestMethod]
    public void Keeps_Number_Defaults()
    {
        Mutate(new JsonSchema { Type = JsonObjectType.Integer, Default = 5L }).Default.Should().Be(5L);
        Mutate(new JsonSchema { Type = JsonObjectType.Number, Default = 2.5 }).Default.Should().Be(2.5);
    }

    [TestMethod]
    public void Keeps_Boolean_Defaults_And_Drops_Them_On_Numeric_Schemas()
    {
        Mutate(new JsonSchema { Type = JsonObjectType.Boolean, Default = true }).Default.Should().Be(true);
        Mutate(new JsonSchema { Type = JsonObjectType.Integer, Default = true }).Default.Should().BeNull();
        Mutate(new JsonSchema { Type = JsonObjectType.Boolean, Default = 1L }).Default.Should().BeNull();
    }

    [TestMethod]
    public void Leaves_Schemas_Without_Default_Unchanged()
    {
        Mutate(new JsonSchema { Type = JsonObjectType.Integer }).Default.Should().BeNull();
    }

    [TestMethod]
    public void Keeps_Integer_Enums_With_Numeric_And_Null_Values()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Integer | JsonObjectType.Null };
        schema.Enumeration.Add(1L);
        schema.Enumeration.Add("2");
        schema.Enumeration.Add(null);

        Mutate(schema).Type.Should().Be(JsonObjectType.Integer | JsonObjectType.Null);
    }

    [TestMethod]
    public void Generates_Integer_Enums_With_Non_Numeric_Values_As_String_Enums()
    {
        var schema = new JsonSchema { Type = JsonObjectType.Integer | JsonObjectType.Null };
        schema.Enumeration.Add(1L);
        schema.Enumeration.Add("0, evil");

        Mutate(schema).Type.Should().Be(JsonObjectType.String | JsonObjectType.Null);
    }

    [TestMethod]
    public void Leaves_String_Enums_Unchanged()
    {
        var schema = new JsonSchema { Type = JsonObjectType.String };
        schema.Enumeration.Add("a\"; evil");

        Mutate(schema).Type.Should().Be(JsonObjectType.String);
    }

    // NJsonSchema maps byte to byte, int64/long to long, uint64/ulong to ulong, int32 to int, and everything else
    // (uint32, int16, ...) to int, so a default is kept only when it fits that type
    [TestMethod]
    [DataRow("byte", "255", true)]
    [DataRow("byte", "300", false)]
    [DataRow("byte", "-1", false)]
    [DataRow("uint32", "3000000000", false)]
    [DataRow("uint32", "2147483647", true)]
    [DataRow("int16", "3000000000", false)]
    [DataRow("int32", "3000000000", false)]
    [DataRow("int64", "3000000000", true)]
    [DataRow("long", "3000000000", true)]
    [DataRow("int64", "9223372036854775808", false)]
    [DataRow("uint64", "9223372036854775808", true)]
    [DataRow("ulong", "18446744073709551615", true)]
    [DataRow("uint64", "18446744073709551616", false)]
    [DataRow("uint64", "-1", false)]
    [DataRow(null, "3000000000", false)]
    [DataRow(null, "42", true)]
    public void Keeps_Integer_Default_Only_When_It_Fits_The_Generated_Type(string? format, string defaultValue, bool kept)
    {
        var schema = Mutate(new JsonSchema { Type = JsonObjectType.Integer, Format = format, Default = defaultValue });

        if (kept)
            schema.Default.Should().Be(defaultValue);
        else
            schema.Default.Should().BeNull();
    }

    [TestMethod]
    public void Keeps_Integer_Default_For_Property_Widened_To_Long_By_Its_Bounds()
    {
        var schema = Mutate(new JsonSchema
        {
            Type = JsonObjectType.Integer,
            Maximum = 4000000000m,
            Default = "3000000000",
        });

        schema.Default.Should().Be("3000000000");
    }

    [TestMethod]
    public void Drops_Integer_Default_When_Bounds_Fit_Int()
    {
        var schema = Mutate(new JsonSchema
        {
            Type = JsonObjectType.Integer,
            Maximum = 100m,
            Default = "3000000000",
        });

        schema.Default.Should().BeNull();
    }

    [TestMethod]
    public void Drops_Integer_Default_When_Format_Prevents_Widening()
    {
        var schema = Mutate(new JsonSchema
        {
            Type = JsonObjectType.Integer,
            Format = "int32",
            Maximum = 4000000000m,
            Default = "3000000000",
        });

        schema.Default.Should().BeNull();
    }

    private static JsonSchema Mutate(JsonSchema schema)
    {
        var document = new OpenApiDocument();
        document.Components.Schemas["Model"] = schema;
        new NonLiteralValueMutator().Mutate(document);
        return schema;
    }
}
