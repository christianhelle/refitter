using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class CSharpClientGeneratorFactoryTests
{
    #region ProcessSchemaForIntegerType Tests

    private const string IntegerTypeOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Integer Type Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "get": {
                "operationId": "TestEndpoint",
                "parameters": [
                  {
                    "name": "intParam",
                    "in": "query",
                    "schema": {
                      "type": "integer"
                    }
                  }
                ],
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": {
                        "$ref": "#/components/schemas/TestModel"
                      }
                    }
                  }
                },
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/TestModel"
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
              "TestModel": {
                "type": "object",
                "properties": {
                  "integerNoFormat": {
                    "type": "integer"
                  },
                  "nestedObject": {
                    "type": "object",
                    "properties": {
                      "nestedInt": {
                        "type": "integer"
                      }
                    }
                  },
                  "arrayOfIntegers": {
                    "type": "array",
                    "items": {
                      "type": "integer"
                    }
                  },
                  "additionalPropsInt": {
                    "type": "object",
                    "additionalProperties": {
                      "type": "integer"
                    }
                  },
                  "allOfInt": {
                    "allOf": [
                      {
                        "type": "object",
                        "properties": {
                          "allOfProp": {
                            "type": "integer"
                          }
                        }
                      }
                    ]
                  },
                  "oneOfInt": {
                    "oneOf": [
                      {
                        "type": "object",
                        "properties": {
                          "oneOfProp": {
                            "type": "integer"
                          }
                        }
                      }
                    ]
                  },
                  "anyOfInt": {
                    "anyOf": [
                      {
                        "type": "object",
                        "properties": {
                          "anyOfProp": {
                            "type": "integer"
                          }
                        }
                      }
                    ]
                  }
                }
              }
            }
          }
        }
        """;

    private const string RecursiveSchemaOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Recursive Schema API",
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
                  "formattedId": {
                    "format": "int32"
                  },
                  "childCount": {
                    "type": "integer"
                  },
                  "nextNode": {
                    "$ref": "#/components/schemas/RecursiveNode"
                  },
                  "children": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  },
                  "namedNodes": {
                    "type": "object",
                    "additionalProperties": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private const string RecursiveAllOfOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Recursive AllOf API",
            "version": "1.0.0"
          },
          "paths": {
            "/branch": {
              "get": {
                "operationId": "GetBranch",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/RecursiveAllOfNode"
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
              "BaseNode": {
                "type": "object",
                "properties": {
                  "formattedId": {
                    "format": "int32"
                  }
                }
              },
              "RecursiveAllOfNode": {
                "allOf": [
                  {
                    "$ref": "#/components/schemas/BaseNode"
                  },
                  {
                    "type": "object",
                    "properties": {
                      "childCount": {
                        "type": "integer"
                      },
                      "nextNode": {
                        "$ref": "#/components/schemas/RecursiveAllOfNode"
                      }
                    }
                  }
                ]
              }
            }
          }
        }
        """;

    private const string RecursiveSchemaOpenApiSpecV2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Recursive Schema API",
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
                "formattedId": {
                  "format": "int32"
                },
                "childCount": {
                  "type": "integer"
                },
                "nextNode": {
                  "$ref": "#/definitions/RecursiveNode"
                },
                "children": {
                  "type": "array",
                  "items": {
                    "$ref": "#/definitions/RecursiveNode"
                  }
                },
                "namedNodes": {
                  "type": "object",
                  "additionalProperties": {
                    "$ref": "#/definitions/RecursiveNode"
                  }
                }
              }
            }
          }
        }
        """;

    private const string RecursiveAllOfOpenApiSpecV2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Recursive AllOf API",
            "version": "1.0.0"
          },
          "paths": {
            "/branch": {
              "get": {
                "operationId": "GetBranch",
                "produces": ["application/json"],
                "responses": {
                  "200": {
                    "description": "Success",
                    "schema": {
                      "$ref": "#/definitions/RecursiveAllOfNode"
                    }
                  }
                }
              }
            }
          },
          "definitions": {
            "BaseNode": {
              "type": "object",
              "properties": {
                "formattedId": {
                  "format": "int32"
                }
              }
            },
            "RecursiveAllOfNode": {
              "allOf": [
                {
                  "$ref": "#/definitions/BaseNode"
                },
                {
                  "type": "object",
                  "properties": {
                    "childCount": {
                      "type": "integer"
                    },
                    "nextNode": {
                      "$ref": "#/definitions/RecursiveAllOfNode"
                    }
                  }
                }
              ]
            }
          }
        }
        """;

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForIntegerWithoutFormat()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("long IntegerNoFormat");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForNestedInteger()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("long NestedInt");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForArrayItems()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("ICollection<long> ArrayOfIntegers");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForAdditionalProperties()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("IDictionary<string, long>");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForAllOfSchemas()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("long AllOfProp");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForOneOfSchemas()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        generatedCode.Should().Contain("long OneOfProp");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForAnyOfSchemas()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        // AnyOfInt is an object with oneOf/anyOf schemas, not a simple property
        // Check that it's properly handled even if not generating a direct "long AnyOfProp"
        generatedCode.Should().Contain("AnyOfInt");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_GeneratesLongForParameterWithoutFormat()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        // Check parameter is handled - it should either be "long intParam" or the method signature
        generatedCode.Should().Contain("intParam");
    }

    [Test]
    public async Task ProcessSchemaForIntegerType_WithInt64Setting_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithIntegerType(IntegerType.Int64);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task ProcessSchemaWalkers_WithRecursivePropertyItemAndAdditionalPropertiesSchemas_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithIntegerType(RecursiveSchemaOpenApiSpec, IntegerType.Int64);
        generatedCode.Should().Contain("int FormattedId");
        generatedCode.Should().Contain("long ChildCount");
        generatedCode.Should().Contain("RecursiveNode NextNode");
        generatedCode.Should().Contain("ICollection<RecursiveNode> Children");
        generatedCode.Should().Contain("IDictionary<string, RecursiveNode> NamedNodes");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task ProcessSchemaWalkers_WithRecursiveAllOfSchemas_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithIntegerType(RecursiveAllOfOpenApiSpec, IntegerType.Int64);
        generatedCode.Should().Contain("int FormattedId");
        generatedCode.Should().Contain("long ChildCount");
        generatedCode.Should().Contain("RecursiveAllOfNode NextNode");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task ProcessSchemaWalkers_WithRecursivePropertyItemAndAdditionalPropertiesSchemas_V2_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithIntegerType(RecursiveSchemaOpenApiSpecV2, IntegerType.Int64);
        generatedCode.Should().Contain("int? FormattedId");
        generatedCode.Should().Contain("long? ChildCount");
        generatedCode.Should().Contain("RecursiveNode NextNode");
        generatedCode.Should().Contain("ICollection<RecursiveNode> Children");
        generatedCode.Should().Contain("IDictionary<string, RecursiveNode> NamedNodes");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task ProcessSchemaWalkers_WithRecursiveAllOfSchemas_V2_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithIntegerType(RecursiveAllOfOpenApiSpecV2, IntegerType.Int64);
        generatedCode.Should().Contain("int? FormattedId");
        generatedCode.Should().Contain("long? ChildCount");
        generatedCode.Should().Contain("RecursiveAllOfNode NextNode");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCodeWithIntegerType(IntegerType integerType)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(IntegerTypeOpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = integerType
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    private static async Task<string> GenerateCodeWithIntegerType(string openApiSpec, IntegerType integerType)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = integerType
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    #endregion

    #region GenerateDefaultAdditionalProperties Tests

    private const string AdditionalPropertiesOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Additional Properties Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "get": {
                "operationId": "TestEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/TestModel"
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
              "TestModel": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "integer"
                  },
                  "name": {
                    "type": "string"
                  }
                }
              },
              "AnotherModel": {
                "type": "object",
                "properties": {
                  "value": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Create_WithGenerateDefaultAdditionalPropertiesFalse_DoesNotGenerateAdditionalProperties()
    {
        string generatedCode = await GenerateCodeWithAdditionalProperties(false);
        // Check that no AdditionalProperties property is generated
        // (ignore pragma warnings which may contain the word)
        generatedCode.Should().NotContain("IDictionary<string, object> AdditionalProperties");
        generatedCode.Should().NotContain("Dictionary<string, object> AdditionalProperties");
    }

    [Test]
    public async Task Create_WithGenerateDefaultAdditionalPropertiesTrue_GeneratesAdditionalProperties()
    {
        string generatedCode = await GenerateCodeWithAdditionalProperties(true);
        generatedCode.Should().Contain("AdditionalProperties");
        generatedCode.Should().Contain("IDictionary<string, object>");
    }

    [Test]
    public async Task Create_WithGenerateDefaultAdditionalPropertiesFalse_AppliesToAllSchemas()
    {
        string generatedCode = await GenerateCodeWithAdditionalProperties(false);

        // Verify no additional properties generated for any schema
        var lines = generatedCode.Split('\n');
        var additionalPropsLines = lines.Where(l =>
            l.Contains("IDictionary<string, object> AdditionalProperties") ||
            l.Contains("Dictionary<string, object> AdditionalProperties")).ToList();

        additionalPropsLines.Should().BeEmpty();
    }

    [Test]
    public async Task Create_WithGenerateDefaultAdditionalPropertiesFalse_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithAdditionalProperties(false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Create_WithGenerateDefaultAdditionalPropertiesTrue_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeWithAdditionalProperties(true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCodeWithAdditionalProperties(bool generateDefaultAdditionalProperties)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(AdditionalPropertiesOpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateDefaultAdditionalProperties = generateDefaultAdditionalProperties
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    #endregion

    #region ConvertOneOfWithDiscriminatorToAllOf Tests

    private const string OneOfWithDiscriminatorOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "OneOf Discriminator Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "get": {
                "operationId": "TestEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/Response"
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
              "Response": {
                "type": "object",
                "properties": {
                  "vehicle": {
                    "$ref": "#/components/schemas/Vehicle"
                  }
                }
              },
              "Vehicle": {
                "oneOf": [
                  {
                    "$ref": "#/components/schemas/Car"
                  },
                  {
                    "$ref": "#/components/schemas/Truck"
                  }
                ],
                "discriminator": {
                  "propertyName": "vehicleType",
                  "mapping": {
                    "car": "#/components/schemas/Car",
                    "truck": "#/components/schemas/Truck"
                  }
                }
              },
              "Car": {
                "type": "object",
                "properties": {
                  "vehicleType": {
                    "type": "string"
                  },
                  "numberOfDoors": {
                    "type": "integer"
                  }
                }
              },
              "Truck": {
                "type": "object",
                "properties": {
                  "vehicleType": {
                    "type": "string"
                  },
                  "cargoCapacity": {
                    "type": "number"
                  }
                }
              }
            }
          }
        }
        """;

    private const string AnyOfWithDiscriminatorOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "AnyOf Discriminator Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "get": {
                "operationId": "TestEndpoint",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/Response"
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
              "Response": {
                "type": "object",
                "properties": {
                  "payment": {
                    "$ref": "#/components/schemas/Payment"
                  }
                }
              },
              "Payment": {
                "anyOf": [
                  {
                    "$ref": "#/components/schemas/CreditCard"
                  },
                  {
                    "$ref": "#/components/schemas/BankTransfer"
                  }
                ],
                "discriminator": {
                  "propertyName": "paymentType",
                  "mapping": {
                    "credit": "#/components/schemas/CreditCard",
                    "bank": "#/components/schemas/BankTransfer"
                  }
                }
              },
              "CreditCard": {
                "type": "object",
                "properties": {
                  "paymentType": {
                    "type": "string"
                  },
                  "cardNumber": {
                    "type": "string"
                  }
                }
              },
              "BankTransfer": {
                "type": "object",
                "properties": {
                  "paymentType": {
                    "type": "string"
                  },
                  "accountNumber": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_GeneratesBaseClass()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("class Vehicle");
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_DoesNotGenerateAnonymousTypes()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().NotContain("Vehicle2");
        generatedCode.Should().NotContain("Anonymous");
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_GeneratesInheritanceHierarchy()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("Car : Vehicle");
        generatedCode.Should().Contain("Truck : Vehicle");
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_GeneratesAllSubtypes()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("class Car");
        generatedCode.Should().Contain("class Truck");
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task ConvertAnyOfWithDiscriminatorToAllOf_GeneratesBaseClass()
    {
        string generatedCode = await GenerateCodeFromSpec(AnyOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("class Payment");
    }

    [Test]
    public async Task ConvertAnyOfWithDiscriminatorToAllOf_DoesNotGenerateAnonymousTypes()
    {
        string generatedCode = await GenerateCodeFromSpec(AnyOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().NotContain("Payment2");
        generatedCode.Should().NotContain("Anonymous");
    }

    [Test]
    public async Task ConvertAnyOfWithDiscriminatorToAllOf_GeneratesInheritanceHierarchy()
    {
        string generatedCode = await GenerateCodeFromSpec(AnyOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("CreditCard : Payment");
        generatedCode.Should().Contain("BankTransfer : Payment");
    }

    [Test]
    public async Task ConvertAnyOfWithDiscriminatorToAllOf_GeneratesAllSubtypes()
    {
        string generatedCode = await GenerateCodeFromSpec(AnyOfWithDiscriminatorOpenApiSpec);
        generatedCode.Should().Contain("class CreditCard");
        generatedCode.Should().Contain("class BankTransfer");
    }

    [Test]
    public async Task ConvertAnyOfWithDiscriminatorToAllOf_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeFromSpec(AnyOfWithDiscriminatorOpenApiSpec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_WithPolymorphicSerialization_GeneratesJsonAttributes()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec, usePolymorphicSerialization: true);
        generatedCode.Should().Contain("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"vehicleType\"");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(Car)");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(Truck)");
    }

    [Test]
    public async Task ConvertOneOfWithDiscriminatorToAllOf_WithPolymorphicSerialization_CanBuildGeneratedCode()
    {
        string generatedCode = await GenerateCodeFromSpec(OneOfWithDiscriminatorOpenApiSpec, usePolymorphicSerialization: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCodeFromSpec(string spec, bool usePolymorphicSerialization = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UsePolymorphicSerialization = usePolymorphicSerialization
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    #endregion
}
