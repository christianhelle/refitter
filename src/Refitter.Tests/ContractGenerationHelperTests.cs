using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class ContractGenerationHelperTests
{
    private static ContractGenerator CreateGenerator(Action<CodeGeneratorSettings>? configure = null)
    {
        var document = ApiDocumentLoader.Load(
            """{ "openapi": "3.0.1", "info": { "title": "T", "version": "1" }, "paths": {} }""",
            null,
            isYaml: false);
        var codeGeneratorSettings = new CodeGeneratorSettings();
        configure?.Invoke(codeGeneratorSettings);
        return new ContractGeneratorFactory(new RefitGeneratorSettings { CodeGeneratorSettings = codeGeneratorSettings }, document).Create();
    }

    private static string Resolve(ApiSchema schema, bool isNullable = false, Action<CodeGeneratorSettings>? configure = null) =>
        CreateGenerator(configure).Resolver.Resolve(schema, isNullable, "Hint");

    [Test]
    [Arguments("date", "string", false, "System.DateTimeOffset")]
    [Arguments("date", "string", true, "System.DateTimeOffset?")]
    [Arguments("date-time", "string", true, "System.DateTimeOffset?")]
    [Arguments("time", "string", true, "System.TimeSpan?")]
    [Arguments("duration", "string", true, "System.TimeSpan?")]
    [Arguments("uri", "string", false, "System.Uri")]
    [Arguments("uuid", "string", true, "System.Guid?")]
    [Arguments("byte", "string", false, "byte[]")]
    [Arguments("byte", "integer", true, "byte?")]
    [Arguments("ulong", "integer", false, "ulong")]
    [Arguments("float", "number", true, "float?")]
    public void Resolves_Formats(string format, string type, bool isNullable, string expected)
    {
        var schema = new ApiSchema { Type = ApiObjectTypeExtensions.Parse(type), Format = format };

        Resolve(schema, isNullable).Should().Be(expected);
    }

    [Test]
    public void Resolves_Nullable_String_Dates_As_Strings()
    {
        var schema = new ApiSchema { Type = ApiObjectType.String, Format = "date" };

        Resolve(schema, isNullable: true, s => s.DateType = "string").Should().Be("string");
    }

    [Test]
    public void Resolves_Integers_Outside_The_Int_Range_As_Longs()
    {
        Resolve(new ApiSchema { Type = ApiObjectType.Integer, Maximum = 5_000_000_000 }).Should().Be("long");
        Resolve(new ApiSchema { Type = ApiObjectType.Integer, Minimum = 1, Maximum = 10 }).Should().Be("int");
    }

    [Test]
    public void Resolves_Numbers_Without_A_Configured_Type_As_Doubles()
    {
        var generator = CreateGenerator();
        generator.Settings.NumberDecimalType = " ";

        generator.Resolver.Resolve(new ApiSchema { Type = ApiObjectType.Number, Format = "decimal" }, false, null)
            .Should().Be("double");
    }

    [Test]
    public void Resolves_Tuples_And_Untyped_Arrays()
    {
        var tuple = new ApiSchema { Type = ApiObjectType.Array };
        tuple.Items.Add(new ApiSchema { Type = ApiObjectType.String });
        tuple.Items.Add(new ApiSchema { Type = ApiObjectType.Boolean });

        Resolve(tuple).Should().Be("System.Tuple<string, bool>");
        Resolve(new ApiSchema { Type = ApiObjectType.Array }).Should().Be("System.Collections.Generic.ICollection<object>");
    }

    [Test]
    public void Resolves_Dictionaries_From_Pattern_Properties_And_Keys()
    {
        var dictionary = new ApiSchema
        {
            Type = ApiObjectType.Object,
            AllowAdditionalProperties = false,
            DictionaryKey = new ApiSchema { Type = ApiObjectType.Integer },
        };
        dictionary.AdditionalPropertiesSchema = new ApiSchema { Type = ApiObjectType.String };

        Resolve(dictionary).Should().Be("System.Collections.Generic.IDictionary<int, string>");
    }

    [Test]
    public void Resolves_Enumerations_Without_A_Type_As_Named_Types()
    {
        var schema = new ApiSchema();
        schema.Enumeration.Add(1L);

        Resolve(schema, isNullable: true).Should().Be("Hint?");
        FluentActions.Invoking(() => Resolve(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Registers_Only_Definitions_That_Are_Types()
    {
        var resolver = CreateGenerator().Resolver;
        var type = new ApiSchema { Type = ApiObjectType.Object };
        type.Properties["a"] = new ApiSchemaProperty { Type = ApiObjectType.String };

        resolver.RegisterSchemaDefinitions(null);
        resolver.RegisterSchemaDefinitions(new Dictionary<string, ApiSchema>
        {
            ["Type"] = type,
            ["Text"] = new ApiSchema { Type = ApiObjectType.String },
        });

        resolver.IsRegistered(type).Should().BeTrue();
        resolver.Types.Should().ContainSingle(t => t.Value == "Type");
    }

    [Test]
    public void Safe_Type_Names_Fall_Back_To_Anonymous()
    {
        var generator = new SafeContractTypeNameGenerator(new HashSet<string> { "Pet" });

        generator.Generate(new ApiSchema(), "pet", ["Pet"]).Should().Be("Pet2");
        generator.Generate(new ApiSchema(), null, Array.Empty<string>()).Should().Be("Anonymous");
    }

    [Test]
    public void Schema_Type_Names_Use_Document_Paths_Titles_And_Generic_Hints()
    {
        var generator = new SchemaTypeNameGenerator();

        generator.Generate(new ApiSchema { DocumentPath = "c:\\specs\\Owner" }, null, Array.Empty<string>()).Should().Be("Owner");
        generator.Generate(new ApiSchema { Title = "Titled" }, null, Array.Empty<string>()).Should().Be("Titled");
        generator.Generate(new ApiSchema(), "List[Pet]", Array.Empty<string>()).Should().Be("ListOfPet");
        generator.Generate(new ApiSchema(), "Pair<A,B>", Array.Empty<string>()).Should().Be("PairOfAAndB");
        generator.Generate(new ApiSchema(), "object", ["Object"]).Should().Be("Object2");
        generator.Generate(new ApiSchema(), "Pet", ["Pet", "Pet2"]).Should().Be("Pet3");
        generator.Generate(new ApiSchema(), string.Empty, ["Anonymous"]).Should().Be("Anonymous2");
        generator.Generate(new ApiSchema(), "1st", Array.Empty<string>()).Should().Be("_1st");
        generator.Generate(new ApiSchema(), "_x", Array.Empty<string>()).Should().Be("_x");
    }

    [Test]
    public void Converts_Identifiers_And_Literals()
    {
        ConversionUtilities.ConvertToLowerCamelCase(null, firstCharacterMustBeAlpha: false).Should().BeEmpty();
        ConversionUtilities.ConvertToUpperCamelCase("my-name_is", firstCharacterMustBeAlpha: true).Should().Be("MyName_is");
        ConversionUtilities.ConvertToLowerCamelCase("My.Name", firstCharacterMustBeAlpha: false).Should().Be("my.Name");
        ConversionUtilities.ConvertToUpperCamelCase("1st", firstCharacterMustBeAlpha: true).Should().Be("_1st");

        ConversionUtilities.ConvertToStringLiteral("\\\0\a\b\f\n\r\t\v\"'", "\"", "\"")
            .Should().Be("\"\\\\\\0\\a\\b\\f\\n\\r\\t\\v\\\"\\'\"");

        ConversionUtilities.Tab(null, 1).Should().BeEmpty();
        ConversionUtilities.Tab("a\nb", 0).Should().Be("a\nb");
        ConversionUtilities.Tab("a\nb", 2).Should().Be("a\n        b");
        ConversionUtilities.ConvertCSharpDocs(null, 1).Should().BeEmpty();
        ConversionUtilities.ConvertCSharpDocs("a\nb", 3).Should().Contain("/// <br/>");
    }

    [Test]
    public void Generates_Default_Values()
    {
        var generator = CreateGenerator(s =>
        {
            s.ArrayInstanceType = "System.Collections.ObjectModel.Collection";
            s.DictionaryInstanceType = "System.Collections.Generic.Dictionary";
        });
        var values = generator.Settings.ValueGenerator;
        var resolver = generator.Resolver;

        var date = new ApiSchema { Type = ApiObjectType.String, Format = "date-time", Default = "2024-01-02" };
        values.GetDefaultValue(date, false, "System.DateTime", null, true, resolver).Should().Be("System.DateTime.Parse(\"2024-01-02\")");

        var array = new ApiSchema { Type = ApiObjectType.Array };
        values.GetDefaultValue(array, false, "System.Collections.Generic.ICollection<string>", null, true, resolver)
            .Should().Be("new System.Collections.ObjectModel.Collection<string>()");

        var dictionary = new ApiSchema { Type = ApiObjectType.Object };
        values.GetDefaultValue(dictionary, false, "System.Collections.Generic.IDictionary<string, object>", null, true, resolver)
            .Should().Be("new System.Collections.Generic.Dictionary<string, object>()");

        var abstractSchema = new ApiSchema { Type = ApiObjectType.Object, IsAbstract = true };
        values.GetDefaultValue(abstractSchema, false, "Base", null, true, resolver).Should().BeNull();

        var enumeration = new ApiSchema { Type = ApiObjectType.String, Default = "b" };
        enumeration.Enumeration.Add("a");
        enumeration.Enumeration.Add("b");
        enumeration.EnumerationNames.Add("Alpha");
        enumeration.EnumerationNames.Add("Beta");
        values.GetDefaultValue(enumeration, false, "Letters", "Letters", true, resolver).Should().EndWith(".Letters.Beta");
    }

    [Test]
    [Arguments("byte", "(byte)1")]
    [Arguments("float", "1F")]
    [Arguments("decimal", "1M")]
    public void Generates_Numeric_Literals(string format, string expected)
    {
        CreateGenerator().Settings.ValueGenerator.GetNumericValue(ApiObjectType.Number, 1L, format).Should().Be(expected);
    }

    [Test]
    public void Converts_Numbers_To_Strings()
    {
        object[] values = [(byte)1, (sbyte)-1, (short)2, (ushort)3, 4, 5u, 6ul, 1.5f, 2.5d, 3.5m, "7.25", "x"];

        values.Select(ContractValueGenerator.ConvertNumberToString)
            .Should().Equal("1", "-1", "2", "3", "4", "5", "6", "1.5", "2.5", "3.5", "7.25", "x");
    }
}
