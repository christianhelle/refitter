using System.Reflection;
using FluentAssertions;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using Refitter.Core;

namespace Refitter.Tests;

public class CustomCSharpTypeResolverTests
{
    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    [Arguments("Nullable<Guid>")]
    [Arguments("System.Nullable<Guid>")]
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

    [Test]
    [Arguments("System.Guid")]
    [Arguments("System.DateTime")]
    [Arguments("System.DateTimeOffset")]
    [Arguments("System.TimeSpan")]
    [Arguments("System.DateOnly")]
    [Arguments("System.TimeOnly")]
    [Arguments("System.Decimal")]
    [Arguments("System.Int32")]
    [Arguments("System.Int64")]
    [Arguments("System.Double")]
    [Arguments("System.Single")]
    [Arguments("System.Boolean")]
    [Arguments("System.Byte")]
    [Arguments("System.SByte")]
    [Arguments("System.Int16")]
    [Arguments("System.UInt16")]
    [Arguments("System.UInt32")]
    [Arguments("System.UInt64")]
    [Arguments("System.Char")]
    [Arguments("Guid")]
    [Arguments("DateTime")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("DateOnly")]
    [Arguments("TimeOnly")]
    [Arguments("Decimal")]
    [Arguments("Int32")]
    [Arguments("Char")]
    [Arguments("Int64")]
    [Arguments("Double")]
    [Arguments("Single")]
    [Arguments("Boolean")]
    [Arguments("Byte")]
    [Arguments("SByte")]
    [Arguments("Int16")]
    [Arguments("UInt16")]
    [Arguments("UInt32")]
    [Arguments("UInt64")]
    [Arguments("ushort")]
    [Arguments("bool")]
    [Arguments("byte")]
    [Arguments("sbyte")]
    [Arguments("char")]
    [Arguments("decimal")]
    [Arguments("double")]
    [Arguments("float")]
    [Arguments("int")]
    [Arguments("uint")]
    [Arguments("long")]
    [Arguments("ulong")]
    [Arguments("short")]
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

    [Test]
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

    [Test]
    public void Resolve_With_Null_Mappings_Falls_Through()
    {
        var settings = new CSharpGeneratorSettings();
        var resolver = new CustomCSharpTypeResolver(settings, formatMappings: null);
        var schema = new JsonSchema { Type = JsonObjectType.Integer, Format = "int32" };

        var result = resolver.Resolve(schema, isNullable: false, typeNameHint: null);

        result.Should().NotBeNullOrEmpty();
    }

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    [Arguments("System.Guid")]
    [Arguments("System.DateTime")]
    [Arguments("System.DateTimeOffset")]
    [Arguments("System.TimeSpan")]
    [Arguments("System.DateOnly")]
    [Arguments("System.TimeOnly")]
    [Arguments("System.Decimal")]
    [Arguments("System.Int32")]
    [Arguments("System.Int64")]
    [Arguments("System.Double")]
    [Arguments("System.Single")]
    [Arguments("System.Boolean")]
    [Arguments("System.Byte")]
    [Arguments("System.SByte")]
    [Arguments("System.Int16")]
    [Arguments("System.UInt16")]
    [Arguments("System.UInt32")]
    [Arguments("System.UInt64")]
    [Arguments("System.Char")]
    [Arguments("Guid")]
    [Arguments("DateTime")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("DateOnly")]
    [Arguments("TimeOnly")]
    [Arguments("Decimal")]
    [Arguments("Int32")]
    [Arguments("Int64")]
    [Arguments("Double")]
    [Arguments("Single")]
    [Arguments("Boolean")]
    [Arguments("Byte")]
    [Arguments("SByte")]
    [Arguments("Int16")]
    [Arguments("UInt16")]
    [Arguments("UInt32")]
    [Arguments("UInt64")]
    [Arguments("Char")]
    [Arguments("bool")]
    [Arguments("byte")]
    [Arguments("sbyte")]
    [Arguments("char")]
    [Arguments("decimal")]
    [Arguments("double")]
    [Arguments("float")]
    [Arguments("int")]
    [Arguments("uint")]
    [Arguments("long")]
    [Arguments("ulong")]
    [Arguments("short")]
    [Arguments("ushort")]
    public void IsValueType_Returns_True_For_All_Known_Mappings(string mappedType)
    {
        var method = typeof(CustomCSharpTypeResolver).GetMethod(
            "IsValueType",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (bool?)method?.Invoke(null, [mappedType]);

        result.Should().BeTrue();
    }

    [Test]
    [Arguments("string")]
    [Arguments("List<int>")]
    [Arguments("System.String")]
    [Arguments("System.GuidValue")]
    [Arguments("System.DateTimer")]
    [Arguments("System.DateTimeOffsets")]
    [Arguments("System.TimeSpans")]
    [Arguments("System.Decimals")]
    [Arguments("System.Int33")]
    [Arguments("System.Int128")]
    [Arguments("System.DoubleValue")]
    [Arguments("System.SingleValue")]
    [Arguments("System.BooleanValue")]
    [Arguments("System.Bytes")]
    [Arguments("System.SBytes")]
    [Arguments("System.Int24")]
    [Arguments("System.UInt24")]
    [Arguments("System.UInt128")]
    [Arguments("System.Chars")]
    [Arguments("GuidValue")]
    [Arguments("DateTimer")]
    [Arguments("DateTimeOffsets")]
    [Arguments("TimeSpans")]
    [Arguments("Decimals")]
    [Arguments("Int33")]
    [Arguments("Int128")]
    [Arguments("DoubleValue")]
    [Arguments("SingleValue")]
    [Arguments("BooleanValue")]
    [Arguments("Bytes")]
    [Arguments("SBytes")]
    [Arguments("Int24")]
    [Arguments("UInt24")]
    [Arguments("UInt128")]
    [Arguments("Chars")]
    [Arguments("boolean")]
    [Arguments("bytes")]
    [Arguments("sbytes")]
    [Arguments("character")]
    [Arguments("decimals")]
    [Arguments("doubleValue")]
    [Arguments("floating")]
    [Arguments("integer")]
    [Arguments("unsigned")]
    [Arguments("longValue")]
    [Arguments("ulongValue")]
    [Arguments("shortValue")]
    public void IsValueType_Returns_False_For_Reference_Types(string mappedType)
    {
        var method = typeof(CustomCSharpTypeResolver).GetMethod(
            "IsValueType",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = (bool?)method?.Invoke(null, [mappedType]);

        result.Should().BeFalse();
    }
}
