using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Regression;

/// <summary>
/// Regression tests for Issue #1015: NullReferenceException on Swagger 2.0 documents
/// Validates graceful handling when document.Components is null or document.Components.Schemas is null
/// </summary>
public class OneOfDiscriminatorNullRefTests
{
    // Swagger 2.0 uses "definitions" not "components"
    private const string Swagger20Spec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Pet Store API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""operationId"": ""GetPets"",
        ""produces"": [""application/json""],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""$ref"": ""#/definitions/Pet""
              }
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""Pet"": {
      ""type"": ""object"",
      ""properties"": {
        ""id"": {
          ""type"": ""integer"",
          ""format"": ""int64""
        },
        ""name"": {
          ""type"": ""string""
        }
      }
    }
  }
}
";

    // OpenAPI 3.0 without components section at all
    private const string OpenApi30NoComponents = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Minimal API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/health"": {
      ""get"": {
        ""operationId"": ""HealthCheck"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""text/plain"": {
                ""schema"": {
                  ""type"": ""string""
                }
              }
            }
          }
        }
      }
    }
  }
}
";

    // OpenAPI 3.0 with components but no schemas
    private const string OpenApi30NoSchemas = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""API with Security"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/data"": {
      ""get"": {
        ""operationId"": ""GetData"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  },
  ""components"": {
    ""securitySchemes"": {
      ""Bearer"": {
        ""type"": ""http"",
        ""scheme"": ""bearer""
      }
    }
  }
}
";

    [Test]
    public async Task Should_Not_Throw_NRE_On_Swagger_20_Document()
    {
        var act = async () =>
        {
            string generatedCode = await GenerateCode(Swagger20Spec);
            return generatedCode;
        };

        // Should not throw NullReferenceException when accessing document.Components.Schemas
        await act.Should().NotThrowAsync<NullReferenceException>(
            "Swagger 2.0 documents have null Components, code should handle this gracefully");
    }

    [Test]
    public async Task Should_Not_Throw_NRE_On_OpenAPI_Without_Components()
    {
        var act = async () =>
        {
            string generatedCode = await GenerateCode(OpenApi30NoComponents);
            return generatedCode;
        };

        await act.Should().NotThrowAsync<NullReferenceException>(
            "OpenAPI documents without components section should be handled gracefully");
    }

    [Test]
    public async Task Should_Not_Throw_NRE_On_OpenAPI_Without_Schemas()
    {
        var act = async () =>
        {
            string generatedCode = await GenerateCode(OpenApi30NoSchemas);
            return generatedCode;
        };

        await act.Should().NotThrowAsync<NullReferenceException>(
            "OpenAPI documents with components but no schemas should be handled gracefully");
    }

    [Test]
    public async Task Can_Generate_Code_From_Swagger_20()
    {
        string generatedCode = await GenerateCode(Swagger20Spec);

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("GetPets");
    }

    [Test]
    public async Task Can_Generate_Code_From_OpenAPI_Without_Components()
    {
        string generatedCode = await GenerateCode(OpenApi30NoComponents);

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("HealthCheck");
    }

    [Test]
    public async Task Can_Generate_Code_From_OpenAPI_Without_Schemas()
    {
        string generatedCode = await GenerateCode(OpenApi30NoSchemas);

        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("GetData");
    }

    [Test]
    public async Task Can_Build_Generated_Code_From_All_Variants()
    {
        var swagger20Code = await GenerateCode(Swagger20Spec);
        var noComponentsCode = await GenerateCode(OpenApi30NoComponents);
        var noSchemasCode = await GenerateCode(OpenApi30NoSchemas);

        BuildHelper.BuildCSharp(swagger20Code).Should().BeTrue(
            "Swagger 2.0 generated code should compile");

        BuildHelper.BuildCSharp(noComponentsCode).Should().BeTrue(
            "OpenAPI without components generated code should compile");

        BuildHelper.BuildCSharp(noSchemasCode).Should().BeTrue(
            "OpenAPI without schemas generated code should compile");
    }

    private static async Task<string> GenerateCode(string spec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
