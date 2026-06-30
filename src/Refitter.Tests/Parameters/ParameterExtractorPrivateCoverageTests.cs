using System.Text;
using FluentAssertions;
using NJsonSchema;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;

namespace Refitter.Tests.Parameters;


public class ParameterExtractorPrivateCoverageTests
{
    [Test]
    public void GetDefaultValueForParameter_Returns_Default_For_Empty_Parameter_String()
    {
        var result = ParameterDefaultValueFormatter.GetDefaultValueForParameter(
            string.Empty,
            new List<CSharpParameterModel>());

        result.Should().Be("default");
    }

    [Test]
    public void FormatDefaultValue_Returns_Default_For_Null_And_Unsupported_Types()
    {
        var nullResult = ParameterDefaultValueFormatter.FormatDefaultValue(null!, "string");
        var unsupportedTypeResult = ParameterDefaultValueFormatter.FormatDefaultValue(42, "CustomType");

        nullResult.Should().Be("default");
        unsupportedTypeResult.Should().Be("default");
    }

    [Test]
    public void EscapeString_Handles_Vertical_Tab_And_Null_Characters()
    {
        var result = ParameterNaming.EscapeString("before\vbetween\0after");

        result.Should().Be("before\\vbetween\\0after");
    }

    [Test]
    [Arguments("uint")]
    [Arguments("UInt32")]
    public void FormatNumericValue_Appends_U_Suffix_For_UInt_Types(string numericType)
    {
        var result = ParameterDefaultValueFormatter.FormatNumericValue(42, numericType);

        result.Should().Be("42U");
    }

    [Test]
    [Arguments("int")]
    [Arguments("Int32")]
    [Arguments("long")]
    [Arguments("Int64")]
    [Arguments("short")]
    [Arguments("Int16")]
    [Arguments("byte")]
    [Arguments("Byte")]
    [Arguments("decimal")]
    [Arguments("Decimal")]
    [Arguments("float")]
    [Arguments("Single")]
    [Arguments("double")]
    [Arguments("Double")]
    [Arguments("sbyte")]
    [Arguments("SByte")]
    [Arguments("uint")]
    [Arguments("UInt32")]
    [Arguments("ulong")]
    [Arguments("UInt64")]
    [Arguments("ushort")]
    [Arguments("UInt16")]
    public void IsNumericType_Returns_True_For_Supported_Numeric_Types(string numericType)
    {
        var result = ParameterDefaultValueFormatter.IsNumericType(numericType);

        result.Should().BeTrue();
    }

    [Test]
    [Arguments("string")]
    [Arguments("Guid")]
    [Arguments("CustomType")]
    [Arguments("numbers")]
    public void IsNumericType_Returns_False_For_Non_Numeric_Types(string numericType)
    {
        var result = ParameterDefaultValueFormatter.IsNumericType(numericType);

        result.Should().BeFalse();
    }

    [Test]
    public void GetAliasAsAttribute_StringOverload_Escapes_Special_Characters()
    {
        var unchanged = ParameterAttributeFormatter.GetAliasAsAttribute("same", "same");
        var withQuote = ParameterAttributeFormatter.GetAliasAsAttribute("user\"id", "userId");
        var withBackslash = ParameterAttributeFormatter.GetAliasAsAttribute("user\\id", "userId");

        unchanged.Should().BeEmpty();
        withQuote.Should().Be("AliasAs(\"user\\\"id\")");
        withBackslash.Should().Be("AliasAs(\"user\\\\id\")");
    }

    [Test]
    public void GetCSharpType_Handles_Number_Object_Unknown_And_Nullable_String()
    {
        var settings = new RefitGeneratorSettings { OptionalParameters = true };

        var numberType = ParameterTypeResolver.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.Number },
            settings);

        var objectType = ParameterTypeResolver.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.Object },
            settings);

        var unknownType = ParameterTypeResolver.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.None },
            settings);

        var nullableStringType = ParameterTypeResolver.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.String, IsNullableRaw = true },
            settings);

        numberType.Should().Be("double");
        objectType.Should().Be("object");
        unknownType.Should().Be("object");
        nullableStringType.Should().Be("string?");
    }

    [Test]
    public void GetIntegerTypeName_Uses_Explicit_Int_Formats()
    {
        var settings = new RefitGeneratorSettings();

        var int64Type = ParameterTypeResolver.GetIntegerTypeName(
            new JsonSchema { Format = "int64" },
            settings);

        var int32Type = ParameterTypeResolver.GetIntegerTypeName(
            new JsonSchema { Format = "int32" },
            settings);

        int64Type.Should().Be("long");
        int32Type.Should().Be("int");
    }

    [Test]
    public void GetArrayType_Returns_Object_Array_When_Item_Is_Missing()
    {
        var result = ParameterTypeResolver.GetArrayType(
            new JsonSchema { Type = JsonObjectType.Array },
            new RefitGeneratorSettings());

        result.Should().Be("object[]");
    }

    [Test]
    public void ReplaceUnsafeCharacters_Delegates_To_ParameterNaming()
    {
        var result = ParameterNaming.ReplaceUnsafeCharacters("unsafe-name!");
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void ReOrderNullableParameters_Delegates_To_OptionalParameterReorderer()
    {
        var parameterModels = new List<CSharpParameterModel>();
        var result = OptionalParameterReorderer.Reorder(
            new List<string> { "string a", "int? b = default" },
            new RefitGeneratorSettings(),
            parameterModels);

        result.Should().NotBeNull();
    }

    [Test]
    public void FormatDefaultValue_Adds_PointZero_For_Double_Integer_Values()
    {
        var result = ParameterDefaultValueFormatter.FormatDefaultValue(42, "double");
        result.Should().Be("42.0");
    }

    [Test]
    public void JoinAttributes_Returns_Empty_For_Empty_Input()
    {
        var result = ParameterAttributeFormatter.JoinAttributes();
        result.Should().Be(string.Empty);
    }

    [Test]
    public void JoinAttributes_Returns_Combined_Attributes()
    {
        var result = ParameterAttributeFormatter.JoinAttributes("AliasAs(\"name\")", "Query()");
        result.Should().Be("[AliasAs(\"name\"), Query()] ");
    }

    [Test]
    public void JoinAttributes_Returns_Single_Attribute()
    {
        var result = ParameterAttributeFormatter.JoinAttributes("AliasAs(\"name\")");
        result.Should().Be("[AliasAs(\"name\")] ");
    }

    [Test]
    public void FindSupportedType_Passes_Through_Type_Name()
    {
        var result = ParameterTypeResolver.FindSupportedType("string");
        result.Should().Be("string");
    }

    [Test]
    public void ConvertToVariableName_Replaces_Unsafe_Characters()
    {
        var result = ParameterNaming.ConvertToVariableName("field-name");
        result.Should().Be("field_name");
    }

    [Test]
    public void ConvertToVariableName_Returns_Value_For_Empty_String()
    {
        var result = ParameterNaming.ConvertToVariableName(string.Empty);
        result.Should().Be("value");
    }

    [Test]
    public void AppendXmlDocComment_Extends_CodeBuilder()
    {
        var codeBuilder = new StringBuilder();
        DynamicQuerystringParameterBuilder.AppendXmlDocComment("Some description", codeBuilder);
        codeBuilder.ToString().Should().Contain("Some description");
    }
}
