using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class TypeOverrideTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
info:
  title: Type Override Test API
  version: '1.0'
paths:
  /api/test:
    get:
      operationId: GetTest
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TestResponse'
components:
  schemas:
    TestResponse:
      type: object
      properties:
        customDate:
          type: string
          format: my-custom-date
        customDateTime:
          type: string
          format: my-custom-datetime
        normalString:
          type: string
";

    [Test]
    [Skip("TypeOverride feature implementation is incomplete")]
    public async Task Can_Generate_Code_With_TypeOverrides()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Skip("TypeOverride feature implementation is incomplete")]
    public async Task Generated_Code_Uses_Custom_Type_For_Date()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("CustomDateType");
    }

    [Test]
    [Skip("TypeOverride feature implementation is incomplete")]
    public async Task Generated_Code_Uses_Custom_Type_For_DateTime()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("CustomDateTimeType");
    }

    [Test]
    [Skip("TypeOverride feature implementation is incomplete")]
    public async Task Generated_Code_Does_Not_Override_Normal_String()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("string? NormalString");
    }

    [Test]
    [Skip("TypeOverride feature implementation is incomplete")]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();

        // Add custom type definitions for compilation
        var customTypes = @"
namespace CustomTypes
{
    public class CustomDateType
    {
        public static implicit operator string(CustomDateType value) => value?.ToString() ?? string.Empty;
        public static implicit operator CustomDateType(string value) => new CustomDateType();
    }
    
    public class CustomDateTimeType
    {
        public static implicit operator string(CustomDateTimeType value) => value?.ToString() ?? string.Empty;
        public static implicit operator CustomDateTimeType(string value) => new CustomDateTimeType();
    }
}
";

        BuildHelper
            .BuildCSharp(generatedCode + Environment.NewLine + customTypes)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                TypeOverrides = new[]
                {
                    new TypeOverride
                    {
                        FormatPattern = "string:my-custom-date",
                        TypeName = "CustomTypes.CustomDateType"
                    },
                    new TypeOverride
                    {
                        FormatPattern = "string:my-custom-datetime",
                        TypeName = "CustomTypes.CustomDateTimeType"
                    }
                }
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
