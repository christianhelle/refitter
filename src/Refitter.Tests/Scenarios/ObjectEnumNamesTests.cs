using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// x-enumNames, x-enum-names and x-enum-varnames as an object keyed by enum value instead of an array (#1336).
/// </summary>
[TestClass]
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

    [TestMethod]
    [DataRow("x-enumNames")]
    [DataRow("x-enum-names")]
    [DataRow("x-enum-varnames")]
    public async Task Can_Generate_Code(string extensionName)
    {
        var generatedCode = await GenerateCode(extensionName);
        generatedCode.Should().Contain("enum Status");
        generatedCode.Should().Contain("Task<Status> GetStatus(");
    }

    // NJsonSchema only names enum members from x-enumNames; it reads the dashed spellings but does not use them
    [TestMethod]
    public async Task Generates_Enum_Members_Named_From_Object()
    {
        var generatedCode = await GenerateCode("x-enumNames");
        generatedCode.Should().Contain("Pending = 0");
        generatedCode.Should().Contain("Done = 1");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code_When_Some_Values_Have_No_Name()
    {
        var spec = OpenApiSpec.Replace("\"x-enumNames\": { \"1\": \"Done\", \"0\": \"Pending\" }", "\"x-enumNames\": { \"0\": \"Pending\" }");
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("Pending = 0");
        generatedCode.Should().Contain("_1 = 1");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow("x-enumNames")]
    [DataRow("x-enum-names")]
    [DataRow("x-enum-varnames")]
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
