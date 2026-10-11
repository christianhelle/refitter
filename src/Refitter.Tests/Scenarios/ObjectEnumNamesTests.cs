using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// x-enumNames, x-enum-names and x-enum-varnames as an object keyed by enum value instead of an array (#1336).
/// </summary>
public class ObjectEnumNamesTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Repro", "version": "v1" },
          "paths": {
            "/status": {
              "get": {
                "operationId": "GetStatus",
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": { "schema": { "$ref": "#/components/schemas/Status" } }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Status": {
                "type": "integer",
                "enum": [0, 1],
                "x-enumNames": { "1": "Done", "0": "Pending" }
              }
            }
          }
        }
        """;

    [Test]
    [Arguments("x-enumNames")]
    [Arguments("x-enum-names")]
    [Arguments("x-enum-varnames")]
    public async Task Can_Generate_Code(string extensionName)
    {
        var generatedCode = await GenerateCode(extensionName);
        generatedCode.Should().Contain("enum Status");
        generatedCode.Should().Contain("Task<Status> GetStatus(");
    }

    // NJsonSchema only names enum members from x-enumNames; it reads the dashed spellings but does not use them
    [Test]
    public async Task Generates_Enum_Members_Named_From_Object()
    {
        var generatedCode = await GenerateCode("x-enumNames");
        generatedCode.Should().Contain("Pending = 0");
        generatedCode.Should().Contain("Done = 1");
    }

    [Test]
    [Category("Integration")]
    [Arguments("x-enumNames")]
    [Arguments("x-enum-names")]
    [Arguments("x-enum-varnames")]
    public async Task Can_Build_Generated_Code(string extensionName)
    {
        var generatedCode = await GenerateCode(extensionName);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string extensionName)
    {
        var spec = OpenApiSpec.Replace("\"x-enumNames\"", $"\"{extensionName}\"");
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return sut.Generate();
    }
}
