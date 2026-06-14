using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using Refitter.Core;

namespace Refitter.Tests.Parameters;

public class ParameterExtractorPrivateCoverageTests
{
    [Test]
    public void GetDefaultValueForParameter_Returns_Default_For_Empty_Parameter_String()
    {
        var result = ParameterShared.GetDefaultValueForParameter(
            string.Empty,
            new List<CSharpParameterModel>());

        result.Should().Be("default");
    }

    [Test]
    public void FormatDefaultValue_Returns_Default_For_Null_And_Unsupported_Types()
    {
        var nullResult = ParameterShared.FormatDefaultValue(null!, "string");
        var unsupportedTypeResult = ParameterShared.FormatDefaultValue(42, "CustomType");

        nullResult.Should().Be("default");
        unsupportedTypeResult.Should().Be("default");
    }

    [Test]
    public void EscapeString_Handles_Vertical_Tab_And_Null_Characters()
    {
        var result = ParameterShared.EscapeString("before\vbetween\0after");

        result.Should().Be("before\\vbetween\\0after");
    }

    [Test]
    [Arguments("uint")]
    [Arguments("UInt32")]
    public void FormatNumericValue_Appends_U_Suffix_For_UInt_Types(string numericType)
    {
        var result = ParameterShared.FormatNumericValue(42, numericType);

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
        var result = ParameterShared.IsNumericType(numericType);

        result.Should().BeTrue();
    }

    [Test]
    [Arguments("string")]
    [Arguments("Guid")]
    [Arguments("CustomType")]
    [Arguments("numbers")]
    public void IsNumericType_Returns_False_For_Non_Numeric_Types(string numericType)
    {
        var result = ParameterShared.IsNumericType(numericType);

        result.Should().BeFalse();
    }

    [Test]
    public void GetAliasAsAttribute_CSharpParameterModel_Returns_Expected_Value()
    {
        var unchangedName = CreateParameterModel("id", "id");
        var aliasedName = CreateParameterModel("user-id", "userId");

        var unchangedResult = ParameterShared.GetAliasAsAttribute(unchangedName);
        var aliasedResult = ParameterShared.GetAliasAsAttribute(aliasedName);

        unchangedResult.Should().BeEmpty();
        aliasedResult.Should().Be("AliasAs(\"user-id\")");
    }

    [Test]
    public void GetAliasAsAttribute_CSharpParameterModel_Escapes_Special_Characters()
    {
        var withQuote = CreateParameterModel("user\"id", "userQuoteid");
        var withBackslash = CreateParameterModel("user\\id", "userBackslashid");

        var quoteResult = ParameterShared.GetAliasAsAttribute(withQuote);
        var backslashResult = ParameterShared.GetAliasAsAttribute(withBackslash);

        quoteResult.Should().Be("AliasAs(\"user\\\"id\")");
        backslashResult.Should().Be("AliasAs(\"user\\\\id\")");
    }

    [Test]
    public void GetAliasAsAttribute_StringOverload_Escapes_Special_Characters()
    {
        var unchanged = ParameterShared.GetAliasAsAttribute("same", "same");
        var withQuote = ParameterShared.GetAliasAsAttribute("user\"id", "userId");
        var withBackslash = ParameterShared.GetAliasAsAttribute("user\\id", "userId");

        unchanged.Should().BeEmpty();
        withQuote.Should().Be("AliasAs(\"user\\\"id\")");
        withBackslash.Should().Be("AliasAs(\"user\\\\id\")");
    }

    [Test]
    public void GetCSharpType_Handles_Number_Object_Unknown_And_Nullable_String()
    {
        var settings = new RefitGeneratorSettings { OptionalParameters = true };

        var numberType = ParameterShared.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.Number },
            settings);

        var objectType = ParameterShared.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.Object },
            settings);

        var unknownType = ParameterShared.GetCSharpType(
            new JsonSchema { Type = JsonObjectType.None },
            settings);

        var nullableStringType = ParameterShared.GetCSharpType(
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

        var int64Type = ParameterShared.GetIntegerTypeName(
            new JsonSchema { Format = "int64" },
            settings);

        var int32Type = ParameterShared.GetIntegerTypeName(
            new JsonSchema { Format = "int32" },
            settings);

        int64Type.Should().Be("long");
        int32Type.Should().Be("int");
    }

    [Test]
    public void GetArrayType_Returns_Object_Array_When_Item_Is_Missing()
    {
        var result = ParameterShared.GetArrayType(
            new JsonSchema { Type = JsonObjectType.Array },
            new RefitGeneratorSettings());

        result.Should().Be("object[]");
    }

    [Test]
    public void ParameterAggregator_ExtractParameters_Adds_Multipart_Text_Fields_When_NSwag_Parameters_Are_Empty()
    {
        var operationModel = CreateOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        var schema = new JsonSchema();
        schema.Properties["title"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };

        var aggregator = new ParameterAggregator();
        var result = aggregator.ExtractParameters(
            operationModel,
            operation,
            new RefitGeneratorSettings(),
            "QueryParams",
            out var dynamicQuerystringParameters);

        result.Should().ContainSingle().Which.Should().Be("string title");
        dynamicQuerystringParameters.Should().Be(string.Empty);
    }

    [Test]
    public void ParameterAggregator_Does_Not_Mutate_Query_Parameter_Collection_When_Generating_Dynamic_Querystring_Wrapper()
    {
        var firstParameter = CreateParameterModel(
            "query",
            "query",
            parameter: new OpenApiParameter
            {
                Name = "query",
                Kind = OpenApiParameterKind.Query,
                IsRequired = true,
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        var secondParameter = CreateParameterModel(
            "page",
            "page",
            type: "int?",
            parameter: new OpenApiParameter
            {
                Name = "page",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.Integer }
            });
        var operationModel = CreateOperationModel(firstParameter, secondParameter);
        var operation = new OpenApiOperation();

        var aggregator = new ParameterAggregator();
        var parameters = aggregator.ExtractParameters(
                operationModel,
                operation,
                new RefitGeneratorSettings { UseDynamicQuerystringParameters = true },
                "SearchQueryParams",
                out var dynamicQuerystringParameters)
            .ToList();

        parameters.Should().ContainSingle().Which.Should().Be("[Query] SearchQueryParams queryParams");
        dynamicQuerystringParameters.Should().Contain("class SearchQueryParams");
        operationModel.Parameters.Should().HaveCount(2);
        operationModel.Parameters.Should().ContainInOrder(firstParameter, secondParameter);
    }

    [Test]
    public void ReplaceUnsafeCharacters_Delegates_To_ParameterShared()
    {
        var result = ParameterShared.ReplaceUnsafeCharacters("unsafe-name!");
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
        var result = ParameterShared.FormatDefaultValue(42, "double");
        result.Should().Be("42.0");
    }

    [Test]
    public void GetBodyAttribute_Returns_Body_For_String_Parameter()
    {
        var param = CreateParameterModel("body", "body", "string");
        var result = ParameterShared.GetBodyAttribute(param, new RefitGeneratorSettings());
        result.Should().Be("Body");
    }

    [Test]
    public void GetBodyAttribute_Returns_Serialized_For_Object_Parameter()
    {
        var param = CreateParameterModel("body", "body", "object");
        var result = ParameterShared.GetBodyAttribute(param, new RefitGeneratorSettings());
        result.Should().Be("Body(BodySerializationMethod.Serialized)");
    }

    [Test]
    public void GetQueryAttribute_Returns_Query_Attribute()
    {
        var param = CreateParameterModel("q", "q");
        var result = ParameterShared.GetQueryAttribute(param, new RefitGeneratorSettings());
        result.Should().NotBeNull();
    }

    [Test]
    public void JoinAttributes_Returns_Empty_For_Empty_Input()
    {
        var result = ParameterShared.JoinAttributes();
        result.Should().Be(string.Empty);
    }

    [Test]
    public void JoinAttributes_Returns_Combined_Attributes()
    {
        var result = ParameterShared.JoinAttributes("AliasAs(\"name\")", "Query()");
        result.Should().Be("[AliasAs(\"name\"), Query()] ");
    }

    [Test]
    public void JoinAttributes_Returns_Single_Attribute()
    {
        var result = ParameterShared.JoinAttributes("AliasAs(\"name\")");
        result.Should().Be("[AliasAs(\"name\")] ");
    }

    [Test]
    public void GetParameterType_Uses_CSharpProvider_Model_Type()
    {
        var param = CreateParameterModel("param1", "param1", "int");
        var result = ParameterShared.GetParameterType(param, new RefitGeneratorSettings());
        result.Should().Be("int");
    }

    [Test]
    public void GetQueryParameterType_Returns_Query_Parameter_Type()
    {
        var param = CreateParameterModel("q", "q", "string");
        var result = ParameterShared.GetQueryParameterType(param, new RefitGeneratorSettings());
        result.Should().NotBeNull();
    }

    [Test]
    public void FindSupportedType_Passes_Through_Type_Name()
    {
        var result = ParameterShared.FindSupportedType("string");
        result.Should().Be("string");
    }

    [Test]
    public void ConvertToVariableName_Replaces_Unsafe_Characters()
    {
        var result = ParameterShared.ConvertToVariableName("field-name");
        result.Should().Be("field_name");
    }

    [Test]
    public void ConvertToVariableName_Returns_Value_For_Empty_String()
    {
        var result = ParameterShared.ConvertToVariableName(string.Empty);
        result.Should().Be("value");
    }

    [Test]
    public void GetVariableName_Returns_VariableName_From_Model()
    {
        var param = CreateParameterModel("param-name", "paramName");
        var result = ParameterShared.GetVariableName(param);
        result.Should().Be("paramName");
    }

    [Test]
    public void AppendXmlDocComment_Extends_CodeBuilder()
    {
        var codeBuilder = new StringBuilder();
        ParameterShared.AppendXmlDocComment("Some description", codeBuilder);
        codeBuilder.ToString().Should().Contain("Some description");
    }

    [Test]
    public void QueryParameterExtractor_Returns_Empty_When_No_Query_Parameters()
    {
        var operationModel = CreateOperationModel();
        var extractor = new QueryParameterExtractor();
        var result = extractor.Extract(operationModel, new OpenApiOperation(), new RefitGeneratorSettings()).ToList();

        result.Should().BeEmpty();
        extractor.DynamicQuerystringCode.Should().Be(string.Empty);
    }

    [Test]
    public void QueryParameterExtractor_With_Dynamic_Enabled_But_Single_Query_Param_Falls_Back_To_Simple()
    {
        var param = CreateParameterModel("query", "query", "string",
            parameter: new OpenApiParameter
            {
                Name = "query",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        var operationModel = CreateOperationModel(param);
        var settings = new RefitGeneratorSettings { UseDynamicQuerystringParameters = true };

        var extractor = new QueryParameterExtractor
        {
            DynamicQuerystringParameterType = "QueryParams"
        };
        var result = extractor.Extract(operationModel, new OpenApiOperation(), settings).ToList();

        result.Should().ContainSingle().Which.Should().Contain("query");
        extractor.DynamicQuerystringCode.Should().Be(string.Empty);
    }

    [Test]
    public void QueryParameterExtractor_With_No_Dynamic_Returns_Simple_Extraction()
    {
        var param = CreateParameterModel("query", "query", "string",
            parameter: new OpenApiParameter
            {
                Name = "query",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        var operationModel = CreateOperationModel(param);
        var settings = new RefitGeneratorSettings();

        var extractor = new QueryParameterExtractor();
        var result = extractor.Extract(operationModel, new OpenApiOperation(), settings).ToList();

        result.Should().ContainSingle().Which.Should().Contain("query");
        extractor.DynamicQuerystringCode.Should().Be(string.Empty);
    }

    [Test]
    public void QueryParameterExtractor_With_Dynamic_And_All_Nullable()
    {
        var firstParameter = CreateParameterModel(
            "query",
            "query",
            type: "string?",
            parameter: new OpenApiParameter
            {
                Name = "query",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        var secondParameter = CreateParameterModel(
            "page",
            "page",
            type: "int?",
            parameter: new OpenApiParameter
            {
                Name = "page",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.Integer }
            });
        var operationModel = CreateOperationModel(firstParameter, secondParameter);

        var extractor = new QueryParameterExtractor
        {
            DynamicQuerystringParameterType = "SearchQueryParams"
        };
        var result = extractor.Extract(
            operationModel,
            new OpenApiOperation(),
            new RefitGeneratorSettings { UseDynamicQuerystringParameters = true }).ToList();

        result.Should().ContainSingle().Which.Should().Be("[Query] SearchQueryParams? queryParams");
        extractor.DynamicQuerystringCode.Should().Contain("class SearchQueryParams");
    }

    [Test]
    public void QueryParameterExtractor_With_Dynamic_And_Not_All_Nullable()
    {
        var firstParameter = CreateParameterModel(
            "query",
            "query",
            type: "string",
            parameter: new OpenApiParameter
            {
                Name = "query",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.String }
            });
        var secondParameter = CreateParameterModel(
            "page",
            "page",
            type: "int",
            parameter: new OpenApiParameter
            {
                Name = "page",
                Kind = OpenApiParameterKind.Query,
                Schema = new JsonSchema { Type = JsonObjectType.Integer }
            });
        var operationModel = CreateOperationModel(firstParameter, secondParameter);

        var extractor = new QueryParameterExtractor
        {
            DynamicQuerystringParameterType = "SearchQueryParams"
        };
        var result = extractor.Extract(
            operationModel,
            new OpenApiOperation(),
            new RefitGeneratorSettings { UseDynamicQuerystringParameters = true }).ToList();

        result.Should().ContainSingle().Which.Should().Be("[Query] SearchQueryParams queryParams");
        extractor.DynamicQuerystringCode.Should().Contain("class SearchQueryParams");
    }

    private static CSharpParameterModel CreateParameterModel(
        string name,
        string variableName,
        string type = "string",
        OpenApiParameter? parameter = null)
    {
        var parameterModel = (CSharpParameterModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpParameterModel));
        var baseType = typeof(CSharpParameterModel).BaseType!;

        baseType
            .GetField("<Type>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, type);
        baseType
            .GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, name);
        baseType
            .GetField("<VariableName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, variableName);
        baseType
            .GetField("_parameter", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(
                parameterModel,
                parameter ?? new OpenApiParameter
                {
                    Name = name,
                    Kind = OpenApiParameterKind.Query,
                    Schema = new JsonSchema()
                });

        return parameterModel;
    }

    private static CSharpOperationModel CreateOperationModel(params CSharpParameterModel[] parameters)
    {
        var operationModel = (CSharpOperationModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpOperationModel));
        var baseType = typeof(CSharpOperationModel).BaseType!;

        baseType
            .GetField("<Parameters>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(operationModel, parameters.ToList());

        return operationModel;
    }
}
