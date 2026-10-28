using AwesomeAssertions;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Refitter.Core;

namespace Refitter.Tests;


[TestClass]
public class CustomCSharpTypeResolverTests
{
    [TestMethod]
    public void Resolve_With_Format_Mapping_Returns_Mapped_Type()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "int64" };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().Be("long");
    }

    [TestMethod]
    public void Resolve_With_Nullable_Mapping_Returns_Nullable_Type()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "int64" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be("long?");
    }

    [TestMethod]
    public void Resolve_With_Nullable_Mapping_Already_Nullable_Returns_As_Is()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "custom-nullable", "string?" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "custom-nullable" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be("string?");
    }

    [TestMethod]
    public void Resolve_With_Generic_Mapping_Returns_Nullable_Suffix()
    {
        var settings = new CSharpGeneratorSettings
        {
            GenerateNullableReferenceTypes = true
        };
        var formatMappings = new Dictionary<string, string>
        {
            { "list-int", "List<int>" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "list-int" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be("List<int>?");
    }

    [TestMethod]
    public void Resolve_With_Generic_Mapping_And_Nrt_Disabled_Returns_As_Is()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "list-int", "List<int>" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "list-int" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be("List<int>");
    }

    [TestMethod]
    [DataRow("Nullable<Guid>")]
    [DataRow("System.Nullable<Guid>")]
    public void Resolve_With_Already_Nullable_Value_Type_Mapping_Returns_As_Is(string mappedType)
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "guid-nullable", mappedType }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "guid-nullable" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be(mappedType);
    }

    [TestMethod]
    [DataRow("System.Guid")]
    [DataRow("System.DateTime")]
    [DataRow("System.DateTimeOffset")]
    [DataRow("System.TimeSpan")]
    [DataRow("System.Decimal")]
    [DataRow("System.Int32")]
    [DataRow("System.Int64")]
    [DataRow("System.Double")]
    [DataRow("System.Single")]
    [DataRow("System.Boolean")]
    [DataRow("System.Byte")]
    [DataRow("System.SByte")]
    [DataRow("System.Int16")]
    [DataRow("System.UInt16")]
    [DataRow("System.UInt32")]
    [DataRow("System.UInt64")]
    [DataRow("System.Char")]
    [DataRow("Guid")]
    [DataRow("DateTime")]
    [DataRow("DateTimeOffset")]
    [DataRow("TimeSpan")]
    [DataRow("Decimal")]
    [DataRow("Int32")]
    [DataRow("Char")]
    [DataRow("Int64")]
    [DataRow("Double")]
    [DataRow("Single")]
    [DataRow("Boolean")]
    [DataRow("Byte")]
    [DataRow("SByte")]
    [DataRow("Int16")]
    [DataRow("UInt16")]
    [DataRow("UInt32")]
    [DataRow("UInt64")]
    [DataRow("ushort")]
    [DataRow("bool")]
    [DataRow("byte")]
    [DataRow("sbyte")]
    [DataRow("char")]
    [DataRow("decimal")]
    [DataRow("double")]
    [DataRow("float")]
    [DataRow("int")]
    [DataRow("uint")]
    [DataRow("long")]
    [DataRow("ulong")]
    [DataRow("short")]
    public void Resolve_With_Known_Value_Type_Mappings_Appends_Nullable_Suffix(string mappedType)
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "mapped-format", mappedType }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Format = "mapped-format" };

        var result = resolver.Resolve(schema, isNullable: true, typeNameHint: null);

        result.Should().Be($"{mappedType}?");
    }

    [TestMethod]
    public void Resolve_Without_Matching_Format_Falls_Through()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "date-time" };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe("long");
    }

    [TestMethod]
    public void Resolve_With_Null_Mappings_Falls_Through()
    {
        var settings = new CSharpGeneratorSettings();
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings: null);
        var schema = new JsonSchema { Type = JsonObjectType.Integer, Format = "int32" };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Resolve_With_Empty_Format_Falls_Through()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Type = JsonObjectType.String, Format = "" };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Resolve_With_Null_Format_Falls_Through()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);
        var schema = new JsonSchema { Type = JsonObjectType.String };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Resolve_With_Multiple_Mappings_Returns_Correct_Type()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" },
            { "int32", "int" },
            { "double", "double" },
            { "uuid", "Guid" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);

        resolver.Resolve(new JsonSchema { Format = "int64" }, false, null).Should().Be("long");
        resolver.Resolve(new JsonSchema { Format = "int32" }, false, null).Should().Be("int");
        resolver.Resolve(new JsonSchema { Format = "double" }, false, null).Should().Be("double");
        resolver.Resolve(new JsonSchema { Format = "uuid" }, false, null).Should().Be("Guid");
    }

    [TestMethod]
    public void Resolve_Nullable_With_Multiple_Mappings_Returns_Correct_Nullable_Types()
    {
        var settings = new CSharpGeneratorSettings();
        var formatMappings = new Dictionary<string, string>
        {
            { "int64", "long" },
            { "uuid", "Guid" }
        };
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings);

        resolver.Resolve(new JsonSchema { Format = "int64" }, true, null).Should().Be("long?");
        resolver.Resolve(new JsonSchema { Format = "uuid" }, true, null).Should().Be("Guid?");
    }

}
