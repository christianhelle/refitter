using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class CustomCodeGeneratorWithDateTimeTests
{
    private const string OpenApiSpec =
        """
        {
          "swagger": "2.0",
          "info": {
            "title": "Dummy API",
            "version": "0.0.0"
          },
          "host": "x.io",
          "basePath": "/",
          "schemes": [
            "https"
          ],
          "paths": {
            "/t/dummy/{employee_id}": {
              "get": {
                "summary": "X",
                "description": "X",
                "operationId": "dummy",
                "parameters": [
                  {
                    "name": "employee_id",
                    "in": "path",
                    "description": "the specific employee",
                    "required": true,
                    "format": "int64",
                    "type": "integer"
                  },
                  {
                    "name": "valid_from",
                    "in": "query",
                    "description": "the start of the period",
                    "required": true,
                    "format": "date",
                    "type": "string"
                  },
                  {
                    "name": "valid_to",
                    "in": "query",
                    "description": "the end of the period",
                    "required": true,
                    "format": "date",
                    "type": "string"
                  },
                  {
                    "name": "test_time",
                    "in": "query",
                    "description": "test parameter",
                    "required": true,
                    "format": "time",
                    "type": "string"
                  }
                ],
                "responses": {
                  "200": {
                    "description": "No response was specified"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task GeneratedCode_Contains_TimeSpan_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] System.TimeSpan");
    }

    [Test]
    public async Task GeneratedCode_Contains_DateTime_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("System.DateTime");
    }

    [Test]
    public async Task GeneratedCode_NotContains_DateTimeOffset_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("System.DateTimeOffset");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
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
                DateType = "System.DateTime",
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
