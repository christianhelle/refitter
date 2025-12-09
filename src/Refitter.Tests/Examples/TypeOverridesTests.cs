using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class TypeOverridesTests
{
    private const string OpenApiSpec =
        """
        openapi: 3.0.1
        info:
          title: Type Overrides API
          version: 1.0.0
        paths:
          /test:
            post:
              operationId: TestEndpoint
              parameters:
                - name: customDate
                  in: query
                  required: false
                  schema:
                    type: string
                    format: my-date-time
                - name: regularDate
                  in: query
                  required: false
                  schema:
                    type: string
                    format: date-time
                - name: customInteger
                  in: query
                  required: false
                  schema:
                    type: integer
                    format: custom-int
              requestBody:
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/TestRequest'
              responses:
                '200':
                  description: Success
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/TestResponse'
        components:
          schemas:
            TestRequest:
              type: object
              properties:
                customDateProperty:
                  type: string
                  format: my-date-time
                  description: Should map to custom type
                regularDateProperty:
                  type: string
                  format: date-time
                  description: Should use default DateTimeOffset
                customIntegerProperty:
                  type: integer
                  format: custom-int
                  description: Should map to custom integer type
                regularStringProperty:
                  type: string
                  description: Regular string with no format
            TestResponse:
              type: object
              properties:
                customDateProperty:
                  type: string
                  format: my-date-time
                regularDateProperty:
                  type: string
                  format: date-time
                customIntegerProperty:
                  type: integer
                  format: custom-int
        """;

    [Test]
    public async Task Can_Generate_Code_With_Type_Overrides()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Maps_Custom_String_Format_To_Custom_Type()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();
        generatedCode.Should().Contain("Domain.Specific.CustomDateTime");
    }

    [Test]
    public async Task Maps_Custom_Integer_Format_To_Custom_Type()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();
        generatedCode.Should().Contain("Domain.Specific.CustomInteger");
    }

    [Test]
    public async Task Preserves_Default_DateTime_Mapping()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();
        generatedCode.Should().Contain("System.DateTimeOffset RegularDateProperty");
    }

    [Test]
    public async Task Preserves_Regular_String_Type()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();
        generatedCode.Should().Contain("string RegularStringProperty");
    }

    [Test]
    public async Task Works_Without_Type_Overrides()
    {
        string generatedCode = await GenerateCodeWithoutTypeOverrides();
        generatedCode.Should().NotBeNullOrWhiteSpace();
        // Without type overrides, custom formats should fall back to defaults
        generatedCode.Should().Contain("string CustomDateProperty");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Type_Overrides()
    {
        string generatedCode = await GenerateCodeWithTypeOverrides();

        // Add the custom types to make it compile
        var customTypes = """
            
            namespace Domain.Specific
            {
                public class CustomDateTime
                {
                    public System.DateTime Value { get; set; }
                }
                
                public class CustomInteger
                {
                    public int Value { get; set; }
                }
            }
            """;

        var codeWithTypes = generatedCode + customTypes;

        BuildHelper
            .BuildCSharp(codeWithTypes)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCodeWithTypeOverrides()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                TypeOverrides = new Dictionary<string, string>
                {
                    { "string:my-date-time", "Domain.Specific.CustomDateTime" },
                    { "integer:custom-int", "Domain.Specific.CustomInteger" }
                }
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> GenerateCodeWithoutTypeOverrides()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
