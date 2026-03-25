using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class PropertyNamingPolicyTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Property Naming Policy API",
            "version": "1.0.0"
          },
          "paths": {
            "/payments": {
              "get": {
                "operationId": "GetPayment",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/PaymentResponse"
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
              "PaymentResponse": {
                "type": "object",
                "properties": {
                  "payMethod_SumBank": {
                    "type": "number",
                    "format": "double"
                  },
                  "class": {
                    "type": "string"
                  },
                  "1st-payment-method": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Default_PascalCase_Property_Naming()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Default_PascalCase_Property_Naming_Remains_Unchanged()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public double PayMethodSumBank { get; set; }");
        generatedCode.Should().Contain("""[JsonPropertyName("payMethod_SumBank")]""");
    }

    [Test]
    public async Task PreserveOriginal_Emits_Raw_Valid_Identifiers()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("public double payMethod_SumBank { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Escapes_Reserved_Keywords()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("public string @class { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Minimally_Sanitizes_Invalid_Identifiers()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("_1st_payment_method { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Generated_Code_Can_Build()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        PropertyNamingPolicy propertyNamingPolicy = PropertyNamingPolicy.PascalCase)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                PropertyNamingPolicy = propertyNamingPolicy,
            };

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
