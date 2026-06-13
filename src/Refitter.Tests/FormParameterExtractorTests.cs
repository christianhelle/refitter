using System.Runtime.CompilerServices;
using FluentAssertions;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class FormParameterExtractorTests
{
    [Test]
    public void CanExtract_Returns_True_For_FormData()
    {
        var extractor = new FormParameterExtractor();
        extractor.CanExtract(OpenApiParameterKind.FormData).Should().BeTrue();
    }

    [Test]
    public void CanExtract_Returns_False_For_Non_FormData()
    {
        var extractor = new FormParameterExtractor();
        extractor.CanExtract(OpenApiParameterKind.Query).Should().BeFalse();
        extractor.CanExtract(OpenApiParameterKind.Path).Should().BeFalse();
        extractor.CanExtract(OpenApiParameterKind.Header).Should().BeFalse();
        extractor.CanExtract(OpenApiParameterKind.Body).Should().BeFalse();
    }

    [Test]
    public void Extract_Returns_Empty_When_No_FormData_Parameters()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings());
        result.Should().BeEmpty();
    }

    [Test]
    public void Extract_Returns_FormData_Parameters_With_Correct_Format()
    {
        var extractor = new FormParameterExtractor();
        var parameter = CreateFormDataParameterModel("field1", "field1", "string");
        var operationModel = CreateOperationModel(parameter);
        var operation = new OpenApiOperation();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().ContainSingle().Which.Should().Be("string field1");
    }

    [Test]
    public void Extract_Handles_Alias_For_Different_Variable_Name()
    {
        var extractor = new FormParameterExtractor();
        var parameter = CreateFormDataParameterModel("field-name", "field_name", "string");
        var operationModel = CreateOperationModel(parameter);
        var operation = new OpenApiOperation();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().ContainSingle().Which.Should().Be("[AliasAs(\"field-name\")] string field_name");
    }

    [Test]
    public void Extract_Deduplicates_FormData_Parameters_By_Variable_Name()
    {
        var extractor = new FormParameterExtractor();
        var parameter1 = CreateFormDataParameterModel("field1", "field1", "string");
        var parameter2 = CreateFormDataParameterModel("field1", "field1", "int");
        var operationModel = CreateOperationModel(parameter1, parameter2);
        var operation = new OpenApiOperation();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().ContainSingle();
    }

    [Test]
    public void Extract_Extracts_Multipart_Text_Fields_From_RequestBody()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        var schema = new JsonSchema();
        schema.Properties["title"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        schema.Properties["description"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain("string title");
        result.Should().Contain("string description");
    }

    [Test]
    public void Extract_Skips_Binary_Fields_From_Multipart_RequestBody()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        var schema = new JsonSchema();
        schema.Properties["avatar"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String,
            Format = "binary"
        };
        schema.Properties["title"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();

        result.Should().ContainSingle().Which.Should().Be("string title");
    }

    [Test]
    public void Extract_Skips_Array_Of_Binary_From_Multipart_RequestBody()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        var schema = new JsonSchema();
        schema.Properties["files"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.Array,
            Item = new JsonSchema
            {
                Type = JsonObjectType.String,
                Format = "binary"
            }
        };
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().BeEmpty();
    }

    [Test]
    public void Extract_Handles_Missing_RequestBody_Schema()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().BeEmpty();
    }

    [Test]
    public void Extract_Handles_Null_RequestBody_Properties()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        var schema = new JsonSchema();
        operation.RequestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = schema
        };

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().BeEmpty();
    }

    [Test]
    public void Extract_Skips_When_No_Multipart_Content()
    {
        var extractor = new FormParameterExtractor();
        var operationModel = CreateEmptyOperationModel();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody()
        };
        operation.RequestBody.Content["application/json"] = new OpenApiMediaType();

        var result = extractor.Extract(operationModel, operation, new RefitGeneratorSettings()).ToList();
        result.Should().BeEmpty();
    }

    private static CSharpOperationModel CreateEmptyOperationModel()
    {
        var operationModel = (CSharpOperationModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpOperationModel));
        var baseType = typeof(CSharpOperationModel).BaseType!;

        baseType
            .GetField("<Parameters>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(operationModel, new List<CSharpParameterModel>());

        return operationModel;
    }

    private static CSharpOperationModel CreateOperationModel(params CSharpParameterModel[] parameters)
    {
        var operationModel = (CSharpOperationModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpOperationModel));
        var baseType = typeof(CSharpOperationModel).BaseType!;

        baseType
            .GetField("<Parameters>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(operationModel, parameters.ToList());

        return operationModel;
    }

    private static CSharpParameterModel CreateFormDataParameterModel(
        string name,
        string variableName,
        string type = "string")
    {
        var parameterModel = (CSharpParameterModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpParameterModel));
        var baseType = typeof(CSharpParameterModel).BaseType!;

        baseType
            .GetField("<Type>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(parameterModel, type);
        baseType
            .GetField("<Name>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(parameterModel, name);
        baseType
            .GetField("<VariableName>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(parameterModel, variableName);
        baseType
            .GetField("_parameter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(
                parameterModel,
                new OpenApiParameter
                {
                    Name = name,
                    Kind = OpenApiParameterKind.FormData,
                    Schema = new JsonSchema()
                });

        return parameterModel;
    }
}
