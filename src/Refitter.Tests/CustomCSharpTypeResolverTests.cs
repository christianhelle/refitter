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
}
