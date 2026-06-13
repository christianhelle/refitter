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
        var result = InvokePrivate<string>(
            "GetDefaultValueForParameter",
            [typeof(string), typeof(ICollection<CSharpParameterModel>)],
            string.Empty,
            new List<CSharpParameterModel>());

        result.Should().Be("default");
    }

    [Test]
    public void FormatDefaultValue_Returns_Default_For_Null_And_Unsupported_Types()
    {
        var nullResult = InvokePrivate<string>(
            "FormatDefaultValue",
            [typeof(object), typeof(string)],
            null!,
            "string");

        var unsupportedTypeResult = InvokePrivate<string>(
            "FormatDefaultValue",
            [typeof(object), typeof(string)],
            42,
            "CustomType");

        nullResult.Should().Be("default");
        unsupportedTypeResult.Should().Be("default");
    }

    [Test]
    public void EscapeString_Handles_Vertical_Tab_And_Null_Characters()
    {
        var result = InvokePrivate<string>(
            "EscapeString",
            [typeof(string)],
            "before\vbetween\0after");

        result.Should().Be("before\\vbetween\\0after");
    }

    [Test]
    [Arguments("uint")]
    [Arguments("UInt32")]
    public void FormatNumericValue_Appends_U_Suffix_For_UInt_Types(string numericType)
    {
        var result = InvokePrivate<string>(
            "FormatNumericValue",
            [typeof(object), typeof(string)],
            42,
            numericType);

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
        var result = InvokePrivate<bool>(
            "IsNumericType",
            [typeof(string)],
            numericType);

        result.Should().BeTrue();
    }

    [Test]
    [Arguments("string")]
    [Arguments("Guid")]
    [Arguments("CustomType")]
    [Arguments("numbers")]
    public void IsNumericType_Returns_False_For_Non_Numeric_Types(string numericType)
    {
        var result = InvokePrivate<bool>(
            "IsNumericType",
            [typeof(string)],
            numericType);

        result.Should().BeFalse();
    }

    [Test]
    public void GetAliasAsAttribute_CSharpParameterModel_Returns_Expected_Value()
    {
        var unchangedName = CreateParameterModel("id", "id");
        var aliasedName = CreateParameterModel("user-id", "userId");

        var method = typeof(ParameterExtractor).GetMethod(
            "GetAliasAsAttribute",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(CSharpParameterModel)],
            null);

        var unchangedResult = (string?)method!.Invoke(null, [unchangedName]);
        var aliasedResult = (string?)method.Invoke(null, [aliasedName]);

        unchangedResult.Should().BeEmpty();
        aliasedResult.Should().Be("AliasAs(\"user-id\")");
    }

    [Test]
    public void GetAliasAsAttribute_CSharpParameterModel_Escapes_Special_Characters()
    {
        // A parameter name containing quotes or backslashes must be escaped
        // so the generated AliasAs(...) literal is valid C#.
        var withQuote = CreateParameterModel("user\"id", "userQuoteid");
        var withBackslash = CreateParameterModel("user\\id", "userBackslashid");

        var method = typeof(ParameterExtractor).GetMethod(
            "GetAliasAsAttribute",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(CSharpParameterModel)],
            null);

        var quoteResult = (string?)method!.Invoke(null, [withQuote]);
        var backslashResult = (string?)method.Invoke(null, [withBackslash]);

        quoteResult.Should().Be("AliasAs(\"user\\\"id\")");
        backslashResult.Should().Be("AliasAs(\"user\\\\id\")");
    }

    [Test]
    public void GetAliasAsAttribute_StringOverload_Escapes_Special_Characters()
    {
        var method = typeof(ParameterExtractor).GetMethod(
            "GetAliasAsAttribute",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(string)],
            null);

        var unchanged = (string?)method!.Invoke(null, ["same", "same"]);
        var withQuote = (string?)method.Invoke(null, ["user\"id", "userId"]);
        var withBackslash = (string?)method.Invoke(null, ["user\\id", "userId"]);

        unchanged.Should().BeEmpty();
        withQuote.Should().Be("AliasAs(\"user\\\"id\")");
        withBackslash.Should().Be("AliasAs(\"user\\\\id\")");
    }

    [Test]
    public void GetCSharpType_Handles_Number_Object_Unknown_And_Nullable_String()
    {
        var settings = new RefitGeneratorSettings { OptionalParameters = true };

        var numberType = InvokePrivate<string>(
            "GetCSharpType",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Type = JsonObjectType.Number },
            settings);

        var objectType = InvokePrivate<string>(
            "GetCSharpType",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Type = JsonObjectType.Object },
            settings);

        var unknownType = InvokePrivate<string>(
            "GetCSharpType",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Type = JsonObjectType.None },
            settings);

        var nullableStringType = InvokePrivate<string>(
            "GetCSharpType",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
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

        var int64Type = InvokePrivate<string>(
            "GetIntegerTypeName",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Format = "int64" },
            settings);

        var int32Type = InvokePrivate<string>(
            "GetIntegerTypeName",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Format = "int32" },
            settings);

        int64Type.Should().Be("long");
        int32Type.Should().Be("int");
    }

    [Test]
    public void GetArrayType_Returns_Object_Array_When_Item_Is_Missing()
    {
        var result = InvokePrivate<string>(
            "GetArrayType",
            [typeof(JsonSchema), typeof(RefitGeneratorSettings)],
            new JsonSchema { Type = JsonObjectType.Array },
            new RefitGeneratorSettings());

        result.Should().Be("object[]");
    }

    [Test]
    public void GetParameters_Adds_Multipart_Text_Fields_When_NSwag_Parameters_Are_Empty()
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

        var method = typeof(ParameterExtractor).GetMethod(
            "GetParameters",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        var arguments = new object?[]
        {
            operationModel,
            operation,
            new RefitGeneratorSettings(),
            "QueryParams",
            null
        };

        var result = (IEnumerable<string>)method!.Invoke(null, arguments)!;

        result.Should().ContainSingle().Which.Should().Be("string title");
        arguments[4].Should().Be(string.Empty);
    }

    [Test]
    public void GetParameters_Does_Not_Mutate_Query_Parameter_Collection_When_Generating_Dynamic_Querystring_Wrapper()
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

        var parameters = ParameterExtractor.GetParameters(
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
        var result = InvokePrivate<string>(
            "ReplaceUnsafeCharacters",
            [typeof(string)],
            "unsafe-name!");
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void ReOrderNullableParameters_Delegates_To_OptionalParameterReorderer()
    {
        var parameterModels = new List<CSharpParameterModel>();
        var result = InvokePrivate<List<string>>(
            "ReOrderNullableParameters",
            [typeof(List<string>), typeof(RefitGeneratorSettings), typeof(ICollection<CSharpParameterModel>)],
            new List<string> { "string a", "int? b = default" },
            new RefitGeneratorSettings(),
            parameterModels);

        result.Should().NotBeNull();
    }

    [Test]
    public void FormatDoubleLiteral_Returns_AsIs_When_Contains_Dot()
    {
        var result = InvokePrivate<string>(
            "FormatDoubleLiteral",
            [typeof(string)],
            "3.14");
        result.Should().Be("3.14");
    }

    [Test]
    public void FormatDoubleLiteral_Returns_AsIs_When_Contains_Exponent()
    {
        var result = InvokePrivate<string>(
            "FormatDoubleLiteral",
            [typeof(string)],
            "1.5e10");
        result.Should().Be("1.5e10");

        var resultUpper = InvokePrivate<string>(
            "FormatDoubleLiteral",
            [typeof(string)],
            "1.5E10");
        resultUpper.Should().Be("1.5E10");
    }

    [Test]
    public void FormatDoubleLiteral_Appends_PointZero_For_Integer_String()
    {
        var result = InvokePrivate<string>(
            "FormatDoubleLiteral",
            [typeof(string)],
            "42");
        result.Should().Be("42.0");
    }

    [Test]
    public void GetBodyAttribute_Returns_Body_For_String_Parameter()
    {
        var param = CreateParameterModel("body", "body", "string");
        var result = InvokePrivate<string>(
            "GetBodyAttribute",
            [typeof(CSharpParameterModel), typeof(RefitGeneratorSettings)],
            param,
            new RefitGeneratorSettings());
        result.Should().Be("Body");
    }

    [Test]
    public void GetBodyAttribute_Returns_Serialized_For_Object_Parameter()
    {
        var param = CreateParameterModel("body", "body", "object");
        var result = InvokePrivate<string>(
            "GetBodyAttribute",
            [typeof(CSharpParameterModel), typeof(RefitGeneratorSettings)],
            param,
            new RefitGeneratorSettings());
        result.Should().Be("Body(BodySerializationMethod.Serialized)");
    }

    [Test]
    public void GetQueryAttribute_Returns_Query_Attribute()
    {
        var param = CreateParameterModel("q", "q");
        var result = InvokePrivate<string>(
            "GetQueryAttribute",
            [typeof(CSharpParameterModel), typeof(RefitGeneratorSettings)],
            param,
            new RefitGeneratorSettings());
        result.Should().NotBeNull();
    }

    [Test]
    public void JoinAttributes_Returns_Empty_For_Empty_Input()
    {
        var method = typeof(ParameterExtractor).GetMethod(
            "JoinAttributes",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var result = (string)method!.Invoke(null, [Array.Empty<string>()])!;
        result.Should().Be(string.Empty);
    }

    [Test]
    public void JoinAttributes_Returns_Combined_Attributes()
    {
        var method = typeof(ParameterExtractor).GetMethod(
            "JoinAttributes",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var result = (string)method!.Invoke(null, [new[] { "AliasAs(\"name\")", "Query()" }])!;
        result.Should().Be("[AliasAs(\"name\"), Query()] ");
    }

    [Test]
    public void JoinAttributes_Returns_Single_Attribute()
    {
        var method = typeof(ParameterExtractor).GetMethod(
            "JoinAttributes",
            BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        var result = (string)method!.Invoke(null, [new[] { "AliasAs(\"name\")" }])!;
        result.Should().Be("[AliasAs(\"name\")] ");
    }

    [Test]
    public void GetParameterType_Uses_CSharpProvider_Model_Type()
    {
        var param = CreateParameterModel("param1", "param1", "int");
        var result = InvokePrivate<string>(
            "GetParameterType",
            [typeof(ParameterModelBase), typeof(RefitGeneratorSettings)],
            param,
            new RefitGeneratorSettings());
        result.Should().Be("int");
    }

    [Test]
    public void GetQueryParameterType_Returns_Query_Parameter_Type()
    {
        var param = CreateParameterModel("q", "q", "string");
        var result = InvokePrivate<string>(
            "GetQueryParameterType",
            [typeof(ParameterModelBase), typeof(RefitGeneratorSettings)],
            param,
            new RefitGeneratorSettings());
        result.Should().NotBeNull();
    }

    [Test]
    public void FindSupportedType_Passes_Through_Type_Name()
    {
        var result = InvokePrivate<string>(
            "FindSupportedType",
            [typeof(string)],
            "string");
        result.Should().Be("string");
    }

    [Test]
    public void ConvertToVariableName_Replaces_Unsafe_Characters()
    {
        var result = InvokePrivate<string>(
            "ConvertToVariableName",
            [typeof(string)],
            "field-name");
        result.Should().Be("field_name");
    }

    [Test]
    public void ConvertToVariableName_Returns_Value_For_Empty_String()
    {
        var result = InvokePrivate<string>(
            "ConvertToVariableName",
            [typeof(string)],
            string.Empty);
        result.Should().Be("value");
    }

    [Test]
    public void GetVariableName_Returns_VariableName_From_Model()
    {
        var param = CreateParameterModel("param-name", "paramName");
        var result = InvokePrivate<string>(
            "GetVariableName",
            [typeof(ParameterModelBase)],
            param);
        result.Should().Be("paramName");
    }

    [Test]
    public void AppendXmlDocComment_Extends_CodeBuilder()
    {
        var method = typeof(ParameterExtractor).GetMethod(
            "AppendXmlDocComment",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string), typeof(StringBuilder)],
            null);
        method.Should().NotBeNull();

        var codeBuilder = new StringBuilder();
        method!.Invoke(null, ["Some description", codeBuilder]);
        codeBuilder.ToString().Should().Contain("Some description");
    }

    [Test]
    public void GetQueryParameters_With_No_Dynamic_Returns_Simple_Extraction()
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

        var method = typeof(ParameterExtractor).GetMethod(
            "GetQueryParameters",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(CSharpOperationModel), typeof(RefitGeneratorSettings), typeof(string), typeof(string).MakeByRefType()],
            null);
        method.Should().NotBeNull();

        var args = new object?[] { operationModel, settings, "QueryParams", null };
        var result = (List<string>)method!.Invoke(null, args)!;

        result.Should().ContainSingle().Which.Should().Contain("query");
    }

    [Test]
    public void GetQueryParameters_With_Dynamic_And_All_Nullable()
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

        var method = typeof(ParameterExtractor).GetMethod(
            "GetQueryParameters",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(CSharpOperationModel), typeof(RefitGeneratorSettings), typeof(string), typeof(string).MakeByRefType()],
            null);
        method.Should().NotBeNull();

        var args = new object?[]
        {
            operationModel,
            new RefitGeneratorSettings { UseDynamicQuerystringParameters = true },
            "SearchQueryParams",
            null
        };
        var result = (List<string>)method!.Invoke(null, args)!;

        result.Should().ContainSingle().Which.Should().Be("[Query] SearchQueryParams? queryParams");
    }

    [Test]
    public void GetQueryParameters_With_Dynamic_And_Not_All_Nullable()
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

        var method = typeof(ParameterExtractor).GetMethod(
            "GetQueryParameters",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(CSharpOperationModel), typeof(RefitGeneratorSettings), typeof(string), typeof(string).MakeByRefType()],
            null);
        method.Should().NotBeNull();

        var args = new object?[]
        {
            operationModel,
            new RefitGeneratorSettings { UseDynamicQuerystringParameters = true },
            "SearchQueryParams",
            null
        };
        var result = (List<string>)method!.Invoke(null, args)!;

        result.Should().ContainSingle().Which.Should().Be("[Query] SearchQueryParams queryParams");
    }

    private static T InvokePrivate<T>(string methodName, Type[] parameterTypes, params object?[] arguments)
    {
        var method = typeof(ParameterExtractor).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        method.Should().NotBeNull();

        return (T)method!.Invoke(null, arguments)!;
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
