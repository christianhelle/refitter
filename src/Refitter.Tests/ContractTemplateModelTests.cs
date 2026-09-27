using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

/// <summary>
/// The template models are what the contract templates (including custom templates) are rendered with,
/// so their members are tested directly, including the ones the built-in templates do not use.
/// </summary>
public class ContractTemplateModelTests
{
    private static readonly string Spec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "T", "version": "1" },
          "paths": {
            "/upload": {
              "post": {
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": { "type": "object", "properties": { "file": { "type": "string", "format": "binary" } } }
                    }
                  }
                },
                "responses": { "200": { "description": "ok", "content": { "application/octet-stream": { "schema": { "type": "string", "format": "binary" } } } } }
              }
            }
          },
          "components": {
            "schemas": {
              "Exception": { "type": "object", "properties": { "message": { "type": "string" } } },
              "Problem": { "allOf": [{ "$ref": "#/components/schemas/Exception" }], "type": "object" },
              "Pair": { "type": "array", "items": [{ "type": "string" }, { "type": "integer" }] },
              "Names": { "type": "array", "items": { "type": "string" } },
              "Pet": {
                "type": "object",
                "x-custom": true,
                "required": ["name", "count", "nickname"],
                "properties": {
                  "name": { "type": "string", "pattern": "^\"a\"$", "x-tag": 1 },
                  "nickname": { "type": "string", "nullable": true },
                  "optional": { "type": "string" },
                  "count": { "type": "integer", "format": "int32", "minimum": 1, "maximum": 10, "exclusiveMinimum": true, "exclusiveMaximum": true },
                  "big": { "type": "integer", "format": "int64", "minimum": -1e30, "maximum": 1e30 },
                  "unsigned": { "type": "integer", "format": "uint64", "minimum": -5, "maximum": 1e30 },
                  "ratio": { "type": "number", "minimum": 0.5, "maximum": 1.5, "multipleOf": 0.5, "exclusiveMinimum": true, "exclusiveMaximum": true },
                  "price": { "type": "number", "format": "decimal", "minimum": 1 },
                  "plain": { "type": "integer", "maximum": 5 },
                  "odd": { "type": "number", "format": "odd", "minimum": 2 },
                  "petStatus": { "type": "string", "enum": ["a", "b"] },
                  "status": { "type": "string", "enum": ["x"] }
                }
              }
            }
          }
        }
        """;

    private static (ContractGenerator Generator, ApiDocument Document) Create(Action<CodeGeneratorSettings>? configure = null)
    {
        var document = ApiDocumentLoader.Load(Spec, null, isYaml: false);
        var codeGeneratorSettings = new CodeGeneratorSettings
        {
            GenerateDataAnnotations = true,
            JsonConverters = ["MyConverter"],
            JsonSerializerSettingsTransformationMethod = "Configure",
        };
        configure?.Invoke(codeGeneratorSettings);
        var settings = new RefitGeneratorSettings { Namespace = "Contracts", CodeGeneratorSettings = codeGeneratorSettings };
        return (new ContractGeneratorFactory(settings, document).Create(), document);
    }

    private static ClassTemplateModel CreateClass(string name, Action<CodeGeneratorSettings>? configure = null)
    {
        var (generator, document) = Create(configure);
        return new ClassTemplateModel(name, generator.Settings, generator.Resolver, document.Definitions[name], document);
    }

    private static PropertyModel Property(ClassTemplateModel model, string name) =>
        model.Properties.Single(p => p.Name == name);

    [Test]
    public void Class_Model_Exposes_Schema_And_Settings()
    {
        var model = CreateClass("Pet");

        model.IsObject.Should().BeTrue();
        model.ExtensionData.Should().ContainKey("x-custom");
        model.Namespace.Should().Be("Contracts");
        model.AdditionalPropertiesType.Should().Be("object");
        CreateClass("Names").AdditionalPropertiesType.Should().BeNull();
        model.JsonSerializerParameterCode.Should().Be("Configure(new System.Text.Json.JsonSerializerOptions())");
        model.JsonConvertersArrayCode.Should().Be("new System.Text.Json.Serialization.JsonConverter[] { new MyConverter() }");
        model.InheritsExceptionSchema.Should().BeFalse();
    }

    [Test]
    public void Class_Model_Uses_Defaults_Without_Serializer_Settings()
    {
        var model = CreateClass("Pet", s =>
        {
            s.JsonConverters = null;
            s.JsonSerializerSettingsTransformationMethod = null;
        });

        model.JsonSerializerParameterCode.Should().Be("new System.Text.Json.JsonSerializerOptions()");
        model.JsonConvertersArrayCode.Should().BeEmpty();
    }

    [Test]
    public void Class_Model_Describes_Tuples_And_Exception_Inheritance()
    {
        CreateClass("Pair").TupleTypes.Should().Equal("string", "int");

        var problem = CreateClass("Problem");
        problem.InheritsExceptionSchema.Should().BeTrue();
        problem.BaseClassName.Should().Be("System.Exception");
    }

    [Test]
    public void Property_Model_Exposes_Schema_Details()
    {
        var model = CreateClass("Pet");
        var name = Property(model, "name");

        name.ExtensionData.Should().ContainKey("x-tag");
        name.Format.Should().BeNull();
        name.FieldName.Should().Be("_name");
        name.RegularExpressionValue.Should().Be("^\"\"a\"\"$");
        Property(model, "optional").IsNullable.Should().BeFalse();
        CreateClass("Pet", s => s.GenerateOptionalPropertiesAsNullable = true)
            .Properties.Single(p => p.Name == "optional").IsNullable.Should().BeTrue();
    }

    [Test]
    public void Property_Model_Describes_Newtonsoft_Requirements()
    {
        var model = CreateClass("Pet");
        Property(model, "name").JsonPropertyRequiredCode.Should().Be("Newtonsoft.Json.Required.Always");
        Property(model, "nickname").JsonPropertyRequiredCode.Should().Be("Newtonsoft.Json.Required.AllowNull");
        Property(model, "optional").JsonPropertyRequiredCode.Should().StartWith("Newtonsoft.Json.Required.DisallowNull");

        var lenient = CreateClass("Pet", s => s.RequiredPropertiesMustBeDefined = false);
        Property(lenient, "name").JsonPropertyRequiredCode.Should().StartWith("Newtonsoft.Json.Required.DisallowNull");

        var nullable = CreateClass("Pet", s => s.GenerateOptionalPropertiesAsNullable = true);
        Property(nullable, "optional").JsonPropertyRequiredCode.Should().StartWith("Newtonsoft.Json.Required.Default");
    }

    [Test]
    public void Property_Model_Computes_Ranges()
    {
        var model = CreateClass("Pet");

        var count = Property(model, "count");
        count.RangeMinimumValue.Should().Be("2");
        count.RangeMaximumValue.Should().Be("9");

        var big = Property(model, "big");
        big.RangeMinimumValue.Should().Be(long.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L");
        big.RangeMaximumValue.Should().Be(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L");

        var unsigned = Property(model, "unsigned");
        unsigned.RangeMinimumValue.Should().Contain("0");
        unsigned.RangeMaximumValue.Should().Contain(ulong.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var ratio = Property(model, "ratio");
        ratio.RangeMinimumValue.Should().Be("1.0D");
        ratio.RangeMaximumValue.Should().Be("1.0D");

        var price = Property(model, "price");
        price.RangeType.Should().Be("decimal");
        price.RangeMinimumValue.Should().Be("1");
        price.RangeMaximumValue.Should().Be(decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));

        var plain = Property(model, "plain");
        plain.RangeType.Should().BeNull();
        plain.RangeMinimumValue.Should().Be("int.MinValue");
        plain.RangeMaximumValue.Should().Be("5");

        Property(model, "odd").RangeMaximumValue.Should().Be("double.MaxValue");
    }

    [Test]
    public void Property_Model_Names_Enum_Types_After_The_Class()
    {
        var model = CreateClass("Pet");

        Property(model, "petStatus").Type.Should().Be("PetStatus");
        Property(model, "status").Type.Should().Be("PetStatus2");

        var (generator, document) = Create();
        var anonymous = new ClassTemplateModel("Anonymous", generator.Settings, generator.Resolver, document.Definitions["Pet"], document);
        anonymous.Properties.Single(p => p.Name == "status").Type.Should().Be("Status");
    }

    [Test]
    public void Enum_Model_Describes_Values()
    {
        var (generator, _) = Create();
        var schema = new ApiSchema { Type = ApiObjectType.Integer, IsFlagEnumerable = true };
        schema.ExtensionData = new Dictionary<string, object?> { ["x-custom"] = true };
        foreach (var value in new object[] { (byte)1, (sbyte)2, (short)4, (ushort)8, 16, 32u, 64L, 128ul, 256f, 512d, "x" })
            schema.Enumeration.Add(value);

        var model = new EnumTemplateModel("Flags", schema, generator.Settings);
        var items = model.Enums.ToList();

        model.ExtensionData.Should().ContainKey("x-custom");
        items.Select(i => i.InternalValue).Should().Equal("1", "2", "4", "8", "16", "32", "64", "128", "256", "512", "x");
        items[0].OriginalName.Should().Be("_1");

        var named = new ApiSchema { Type = ApiObjectType.String };
        named.Enumeration.Add("a");
        named.EnumerationNames.Add("Alpha");
        new EnumTemplateModel("Named", named, generator.Settings).Enums.Single().OriginalName.Should().Be("Alpha");
    }

    [Test]
    public void File_And_Converter_Models_Describe_The_Document()
    {
        var (generator, document) = Create();
        var file = new ContractFileTemplateModel(string.Empty, document, generator.Settings);

        file.Namespace.Should().Be("Contracts");
        file.GenerateClientClasses.Should().BeFalse();
        file.ExceptionModelClass.Should().Be("Exception");
        file.RequiresFileParameterType.Should().BeTrue();
        file.GenerateFileResponseClass.Should().BeTrue();
        file.ResponseClassNames.Should().BeEmpty();
        file.ExceptionClassNames.Should().BeEmpty();
        new DateFormatConverterTemplateModel(generator.Settings).GenerateDateFormatConverterClass.Should().BeTrue();

        generator.Settings.ExcludedTypeNames = ["FileParameter", "FileResponse", "DateFormatConverter"];
        file.RequiresFileParameterType.Should().BeFalse();
        file.GenerateFileResponseClass.Should().BeFalse();
        new DateFormatConverterTemplateModel(generator.Settings).GenerateDateFormatConverterClass.Should().BeFalse();
    }
}
