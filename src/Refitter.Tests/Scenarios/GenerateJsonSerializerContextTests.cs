using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

public class GenerateJsonSerializerContextTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Users API",
            "version": "1.0.0"
          },
          "paths": {
            "/api/users": {
              "get": {
                "operationId": "getUsers",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "array",
                          "items": {
                            "$ref": "#/components/schemas/User"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "User": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "integer",
                    "format": "int32"
                  },
                  "name": {
                    "type": "string"
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
        var generatedCode = await GenerateCode(generateJsonSerializerContext: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_JsonSerializerContext()
    {
        var generatedCode = await GenerateCode(generateJsonSerializerContext: true);

        generatedCode.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(User))]");
        generatedCode.Should().Contain("internal partial class UsersApiSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
    }

    [Test]
    public async Task Generated_Code_With_JsonSerializerContext_Can_Build()
    {
        var generatedCode = await GenerateCode(generateJsonSerializerContext: true);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_With_Separate_Contracts_Namespace_Can_Build()
    {
        var generatedCode = await GenerateCode(
            generateJsonSerializerContext: true,
            @namespace: "GeneratedCode.Clients",
            contractsNamespace: "GeneratedCode.Contracts");

        generatedCode.Should().Contain("namespace GeneratedCode.Contracts");
        generatedCode.Should().Contain("internal partial class UsersApiSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_JsonSerializerContext_When_Disabled()
    {
        var generatedCode = await GenerateCode(generateJsonSerializerContext: false);

        generatedCode.Should().NotContain("JsonSerializerContext");
        generatedCode.Should().NotContain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(");
    }

    private static async Task<string> GenerateCode(
        bool generateJsonSerializerContext,
        string @namespace = "GeneratedCode",
        string? contractsNamespace = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = @namespace,
                ContractsNamespace = contractsNamespace,
                GenerateJsonSerializerContext = generateJsonSerializerContext,
                Naming = new NamingSettings
                {
                    UseOpenApiTitle = false,
                    InterfaceName = "IUsersApi"
                }
            };

            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
            {
                File.Delete(swaggerFile);
            }

            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
