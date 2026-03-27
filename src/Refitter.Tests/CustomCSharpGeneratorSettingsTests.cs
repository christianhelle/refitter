using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class CustomCSharpGeneratorSettingsTests
{
    private const string RecursiveExcludedTypeOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Recursive Property Naming API",
            "version": "1.0.0"
          },
          "paths": {
            "/nodes": {
              "get": {
                "operationId": "GetNode",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/RecursiveNode"
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
              "RecursiveNode": {
                "type": "object",
                "properties": {
                  "node_id": {
                    "format": "int32"
                  },
                  "class": {
                    "type": "string"
                  },
                  "1st-node": {
                    "type": "string"
                  },
                  "child_count": {
                    "type": "integer"
                  },
                  "next_node": {
                    "$ref": "#/components/schemas/RecursiveNode"
                  },
                  "children": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  },
                  "named_nodes": {
                    "type": "object",
                    "additionalProperties": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  },
                  "external_node": {
                    "$ref": "#/components/schemas/RecursiveExternalNode"
                  }
                }
              },
              "RecursiveExternalNode": {
                "type": "object",
                "properties": {
                  "external_id": {
                    "type": "integer"
                  },
                  "next_node": {
                    "$ref": "#/components/schemas/RecursiveExternalNode"
                  }
                }
              }
            }
          }
        }
        """;

    private const string RecursiveExcludedTypeSwaggerSpec = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Recursive Property Naming API",
            "version": "1.0.0"
          },
          "paths": {
            "/nodes": {
              "get": {
                "operationId": "GetNode",
                "produces": ["application/json"],
                "responses": {
                  "200": {
                    "description": "Success",
                    "schema": {
                      "$ref": "#/definitions/RecursiveNode"
                    }
                  }
                }
              }
            }
          },
          "definitions": {
            "RecursiveNode": {
              "type": "object",
              "properties": {
                "node_id": {
                  "format": "int32"
                },
                "class": {
                  "type": "string"
                },
                "1st-node": {
                  "type": "string"
                },
                "child_count": {
                  "type": "integer"
                },
                "next_node": {
                  "$ref": "#/definitions/RecursiveNode"
                },
                "children": {
                  "type": "array",
                  "items": {
                    "$ref": "#/definitions/RecursiveNode"
                  }
                },
                "named_nodes": {
                  "type": "object",
                  "additionalProperties": {
                    "$ref": "#/definitions/RecursiveNode"
                  }
                },
                "external_node": {
                  "$ref": "#/definitions/RecursiveExternalNode"
                }
              }
            },
            "RecursiveExternalNode": {
              "type": "object",
              "properties": {
                "external_id": {
                  "type": "integer"
                },
                "next_node": {
                  "$ref": "#/definitions/RecursiveExternalNode"
                }
              }
            }
          }
        }
        """;

    private const string RecursiveExternalNodeStub = """
        namespace Refitter.Tests.PropertyNamingPolicy;

        public class RecursiveExternalNode
        {
        }
        """;

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain(settings.CodeGeneratorSettings!.DateType);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain(settings.CodeGeneratorSettings!.DateTimeType);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("ICollection<");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateType = "DateTime";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("DateTime");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateTimeType = "DateTime";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("DateTime");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.ArrayType = "System.Collection.Generic.IList";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("System.Collection.Generic.IList<");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_ExcludedTypeNames(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings.ExcludedTypeNames = new[]
        {
            "User"
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().NotContain("class User");
    }

    [Test]
    public async Task Can_Generate_With_ExcludedTypeNames_On_Recursive_Schema_And_PreserveOriginal_Property_Names()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "Refitter.Tests.PropertyNamingPolicy",
            PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64,
                ExcludedTypeNames = new[]
                {
                    "RecursiveExternalNode"
                }
            }
        };

        var generatedCode = await GenerateCode(RecursiveExcludedTypeOpenApiSpec, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("public int node_id { get; set; }");
        generatedCode.Should().Contain("public long child_count { get; set; }");
        generatedCode.Should().Contain("public string @class { get; set; }");
        generatedCode.Should().Contain("public string _1st_node { get; set; }");
        generatedCode.Should().Contain("public RecursiveNode next_node { get; set; }");
        generatedCode.Should().Contain("public ICollection<RecursiveNode> children { get; set; }");
        generatedCode.Should().Contain("public IDictionary<string, RecursiveNode> named_nodes { get; set; }");
        generatedCode.Should().Contain("public RecursiveExternalNode external_node { get; set; }");
        generatedCode.Should().NotContain("class RecursiveExternalNode");
        BuildHelper.BuildCSharp(generatedCode, RecursiveExternalNodeStub).Should().BeTrue();
    }

    [Test]
    public async Task Can_Generate_With_ExcludedTypeNames_On_Recursive_Schema_And_PreserveOriginal_Property_Names_V2()
    {
        var settings = new RefitGeneratorSettings
        {
            Namespace = "Refitter.Tests.PropertyNamingPolicy",
            PropertyNamingPolicy = PropertyNamingPolicy.PreserveOriginal,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64,
                ExcludedTypeNames = new[]
                {
                    "RecursiveExternalNode"
                }
            }
        };

        var generatedCode = await GenerateCode(RecursiveExcludedTypeSwaggerSpec, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("public int? node_id { get; set; }");
        generatedCode.Should().Contain("public long? child_count { get; set; }");
        generatedCode.Should().Contain("public string @class { get; set; }");
        generatedCode.Should().Contain("public string _1st_node { get; set; }");
        generatedCode.Should().Contain("public RecursiveNode next_node { get; set; }");
        generatedCode.Should().Contain("public ICollection<RecursiveNode> children { get; set; }");
        generatedCode.Should().Contain("public IDictionary<string, RecursiveNode> named_nodes { get; set; }");
        generatedCode.Should().Contain("public RecursiveExternalNode external_node { get; set; }");
        generatedCode.Should().NotContain("class RecursiveExternalNode");
        BuildHelper.BuildCSharp(generatedCode, RecursiveExternalNodeStub).Should().BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_Immutable_Records(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CodeGeneratorSettings = new CodeGeneratorSettings { GenerateNativeRecords = true, };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("record Pet");
        generatedCode.Should().Contain("Pet(");
        generatedCode.Should().Contain("[JsonConstructor]");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_CustomTemplates(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CustomTemplateDirectory = "./Templates/";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("/* Example Custom Template Text */");
        generatedCode.Should().Contain("public partial class Pet");
    }


    private static async Task<string> GenerateCode(
        SampleOpenSpecifications version,
        string filename,
        RefitGeneratorSettings settings)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        settings.OpenApiPath = swaggerFile;

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    private static async Task<string> GenerateCode(
        string openApiSpec,
        RefitGeneratorSettings settings)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);

        try
        {
            settings.OpenApiPath = swaggerFile;
            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
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
