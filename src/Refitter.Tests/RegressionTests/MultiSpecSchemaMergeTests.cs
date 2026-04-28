using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

/// <summary>
/// Regression tests for Issue #1016: Multi-spec merge drops schemas
/// Validates that schemas from all OpenAPI specs are preserved during merge
/// </summary>
public class MultiSpecSchemaMergeTests
{
    // First spec: has paths but NO components section
    private const string SpecNoComponents = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""API V1"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/health"": {
      ""get"": {
        ""operationId"": ""HealthCheck"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    // Second spec: has components with schemas
    private const string SpecWithUserSchema = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""User API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/users"": {
      ""get"": {
        ""operationId"": ""GetUsers"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/User""
                  }
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""User"": {
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
}
";

    // Third spec: has components with different schemas
    private const string SpecWithProductSchema = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Product API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""GetProducts"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Product""
                  }
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Product"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""title"": {
            ""type"": ""string""
          },
          ""price"": {
            ""type"": ""number"",
            ""format"": ""double""
          }
        }
      }
    }
  }
}
";

    // Spec with components but no schemas
    private const string SpecComponentsNoSchemas = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Auth API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/login"": {
      ""post"": {
        ""operationId"": ""Login"",
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
    public async Task Should_Preserve_Schemas_When_First_Spec_Has_No_Components()
    {
        var (file1, file2) = await CreateSpecFiles(SpecNoComponents, SpecWithUserSchema);

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2);

        // User schema should be present even though first spec has no components
        generatedCode.Should().Contain("class User");
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Preserve_Schemas_When_First_Spec_Has_Components_But_No_Schemas()
    {
        var (file1, file2) = await CreateSpecFiles(SpecComponentsNoSchemas, SpecWithProductSchema);

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2);

        // Product schema should be present
        generatedCode.Should().Contain("class Product");
    }

    [Test]
    public async Task Should_Include_All_Schemas_In_Generated_Code()
    {
        var (file1, file2) = await CreateSpecFiles(SpecWithUserSchema, SpecWithProductSchema);

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2);

        // Both schemas should be present
        generatedCode.Should().Contain("class User");
        generatedCode.Should().Contain("class Product");
    }

    [Test]
    public async Task Three_Spec_Merge_Should_Preserve_All_Schemas()
    {
        var file1 = await TestFile.CreateSwaggerFile(SpecNoComponents, "spec1.json");
        var file2 = await TestFile.CreateSwaggerFile(SpecWithUserSchema, "spec2.json");
        var file3 = await TestFile.CreateSwaggerFile(SpecWithProductSchema, "spec3.json");

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2, file3);

        // All schemas from specs 2 and 3 should be present
        generatedCode.Should().Contain("class User");
        generatedCode.Should().Contain("class Product");
    }

    [Test]
    public async Task Reverse_Order_Should_Also_Preserve_All_Schemas()
    {
        // Test with schema-bearing spec first, then no-schema spec
        var (file1, file2) = await CreateSpecFiles(SpecWithUserSchema, SpecNoComponents);

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2);

        generatedCode.Should().Contain("class User");
    }

    [Test]
    public async Task Can_Build_Merged_Code_From_Multiple_Specs()
    {
        var (file1, file2, file3) = await CreateThreeSpecFiles();

        string generatedCode = await GenerateCodeFromMultipleSpecs(file1, file2, file3);

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("merged code from multiple specs should compile");
    }

    [Test]
    public async Task Economic_OpenApiPaths_Generate_NonEmpty_Buildable_Code()
    {
        var repositoryRoot = GetRepositoryRoot();
        var productsSpec = Path.Combine(repositoryRoot, "test", "OpenAPI", "v3.0", "economic-products.json");
        var webhooksSpec = Path.Combine(repositoryRoot, "test", "OpenAPI", "v3.0", "economic-webhooks.json");
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = [productsSpec, webhooksSpec],
            Namespace = "Economic"
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotBeNullOrWhiteSpace();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("the real e-conomic multi-spec output should compile");
    }

    private static async Task<(string file1, string file2)> CreateSpecFiles(string spec1, string spec2)
    {
        var file1 = await TestFile.CreateSwaggerFile(spec1, "spec1.json");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "spec2.json");
        return (file1, file2);
    }

    private static async Task<(string file1, string file2, string file3)> CreateThreeSpecFiles()
    {
        var file1 = await TestFile.CreateSwaggerFile(SpecNoComponents, "spec1.json");
        var file2 = await TestFile.CreateSwaggerFile(SpecWithUserSchema, "spec2.json");
        var file3 = await TestFile.CreateSwaggerFile(SpecWithProductSchema, "spec3.json");
        return (file1, file2, file3);
    }

    private static async Task<string> GenerateCodeFromMultipleSpecs(params string[] specFiles)
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = specFiles
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("tests should run from within the repository workspace");
        return directory!.FullName;
    }
}
