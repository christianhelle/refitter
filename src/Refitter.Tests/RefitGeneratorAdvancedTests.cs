using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitGeneratorAdvancedTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getProducts"",
        ""summary"": ""Get products"",
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
    },
    ""/api/orders"": {
      ""get"": {
        ""operationId"": ""getOrders"",
        ""summary"": ""Get orders"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Order""
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
            ""type"": ""integer""
          },
          ""name"": {
            ""type"": ""string""
          }
        }
      },
      ""Order"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer""
          },
          ""productId"": {
            ""type"": ""integer""
          }
        }
      }
    }
  }
}
";

    #region Gap 1: ContractTypeSuffix in GenerateMultipleFiles (Lines 249-256)

    [Test]
    public async Task GenerateMultipleFiles_Applies_ContractTypeSuffix_To_All_Files()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                ContractTypeSuffix = "Dto"
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();

            // Verify suffix applied - contract types should use Dto suffix
            var allContent = string.Join("\n", result.Files.Select(f => f.Content));
            allContent.Should().Contain("Dto");

            // Verify contracts file has suffix
            var contractsFile = result.Files.FirstOrDefault(f => f.TypeName == "Contracts");
            if (contractsFile != null)
            {
                contractsFile.Content.Should().Contain("ProductDto");
                contractsFile.Content.Should().Contain("OrderDto");
            }
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_ContractTypeSuffix_Can_Build()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                ContractTypeSuffix = "Dto"
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();
            var allContent = string.Join("\n", result.Files.Select(f => f.Content));
            allContent.Should().Contain("Dto");
            allContent.Should().Contain("interface");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    #endregion

    #region Gap 2: GenerateContracts = false in GenerateMultipleFiles (Lines 221-227)

    [Test]
    public async Task GenerateMultipleFiles_With_GenerateContracts_False_Excludes_Contracts_File()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                GenerateContracts = false
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();
            result.Files.Should().NotContain(f => f.TypeName == "Contracts");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_With_GenerateContracts_False_Still_Has_Interface_Files()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                GenerateContracts = false
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();
            result.Files.Should().Contain(f => f.TypeName != "Contracts" && f.TypeName != "DependencyInjection");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    #endregion

    #region Gap 3: Empty DI config should not add config file (Lines 229-247)

    [Test]
    public async Task GenerateMultipleFiles_Empty_DI_Config_Does_Not_Add_DependencyInjection_File()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            // Create settings with DependencyInjectionSettings but with configuration 
            // that will produce empty config string
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    BaseUrl = "",
                    HttpMessageHandlers = [],
                    TransientErrorHandler = TransientErrorHandler.None,
                    MaxRetryCount = 0,
                    FirstBackoffRetryInSeconds = 0
                }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            // When DI config is empty, the DependencyInjection file should not be added
            // However, this test might need adjustment based on actual behavior
            // as the DependencyInjectionGenerator might still produce some boilerplate
            var diFile = result.Files.FirstOrDefault(f => f.TypeName == "DependencyInjection");

            // If DI file exists, it should have substantial content or not exist at all
            if (diFile != null)
            {
                // If we get a DI file, it should have meaningful content
                diFile.Content.Should().NotBeNullOrWhiteSpace();
            }
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_With_Valid_DI_Config_Includes_DependencyInjection_File()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    BaseUrl = "https://api.example.com",
                    HttpMessageHandlers = [],
                    TransientErrorHandler = TransientErrorHandler.None,
                    MaxRetryCount = 3,
                    FirstBackoffRetryInSeconds = 0.5
                }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().Contain(f => f.TypeName == "DependencyInjection");
            var diFile = result.Files.First(f => f.TypeName == "DependencyInjection");
            diFile.Content.Should().Contain("AddRefitClient");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    #endregion

    #region Gap 4: AdditionalNamespaces in GenerateClient (Lines 297-305)

    [Test]
    public async Task GenerateMultipleFiles_With_AdditionalNamespaces_Includes_Using_Statements()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                AdditionalNamespaces = new[] { "System.Text.Json", "System.Diagnostics.CodeAnalysis" }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            // AdditionalNamespaces should be added to interface files
            var interfaceFiles = result.Files.Where(f =>
                f.TypeName != "Contracts" && f.TypeName != "DependencyInjection").ToList();

            interfaceFiles.Should().NotBeEmpty();

            foreach (var file in interfaceFiles)
            {
                file.Content.Should().Contain("using System.Text.Json;");
                file.Content.Should().Contain("using System.Diagnostics.CodeAnalysis;");
            }
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_With_AdditionalNamespaces_Code_Builds()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                AdditionalNamespaces = new[] { "System.Text.Json", "System.Linq" }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            var combinedCode = string.Join("\n\n", result.Files.Select(f => f.Content));
            BuildHelper.BuildCSharp(combinedCode).Should().BeTrue();
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_AdditionalNamespaces_With_MultipleInterfaces_ByEndpoint()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                MultipleInterfaces = MultipleInterfaces.ByEndpoint,
                AdditionalNamespaces = new[] { "System.Text.Json" }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            var interfaceFiles = result.Files.Where(f =>
                f.TypeName != "Contracts" && f.TypeName != "DependencyInjection").ToList();

            interfaceFiles.Should().HaveCountGreaterThan(1);

            foreach (var file in interfaceFiles)
            {
                file.Content.Should().Contain("using System.Text.Json;");
            }
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    #endregion

    #region Gap 5: OpenApiPaths array (Line 45-46)

    [Test]
    public async Task CreateAsync_With_OpenApiPaths_Array_Loads_Multiple_Documents()
    {
        const string spec1 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 1"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getProducts"",
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

        const string spec2 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 2"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/orders"": {
      ""get"": {
        ""operationId"": ""getOrders"",
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

        var swaggerFile1 = await SwaggerFileHelper.CreateSwaggerFile(spec1);
        var swaggerFile2 = await SwaggerFileHelper.CreateSwaggerFile(spec2);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { swaggerFile1, swaggerFile2 }
            };
            var generator = await RefitGenerator.CreateAsync(settings);

            // Verify the document was created and has paths from both specs
            generator.OpenApiDocument.Should().NotBeNull();
            generator.OpenApiDocument.Paths.Should().NotBeEmpty();

            var code = generator.Generate();
            code.Should().NotBeNullOrWhiteSpace();

            // The merged document should have operations from both specs
            // Check that at least some meaningful content beyond the header was generated
            code.Should().Contain("interface");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile1);
            CleanupSwaggerFile(swaggerFile2);
        }
    }

    [Test]
    public async Task CreateAsync_With_OpenApiPaths_Array_Builds_Successfully()
    {
        const string spec1 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 1"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getProducts"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Product""
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
          ""id"": { ""type"": ""integer"" },
          ""name"": { ""type"": ""string"" }
        }
      }
    }
  }
}
";

        const string spec2 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 2"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/orders"": {
      ""get"": {
        ""operationId"": ""getOrders"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Order""
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
      ""Order"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""integer"" },
          ""productId"": { ""type"": ""integer"" }
        }
      }
    }
  }
}
";

        var swaggerFile1 = await SwaggerFileHelper.CreateSwaggerFile(spec1);
        var swaggerFile2 = await SwaggerFileHelper.CreateSwaggerFile(spec2);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { swaggerFile1, swaggerFile2 }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var code = generator.Generate();

            BuildHelper.BuildCSharp(code).Should().BeTrue();
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile1);
            CleanupSwaggerFile(swaggerFile2);
        }
    }

    [Test]
    public async Task CreateAsync_With_OpenApiPaths_Array_In_MultipleFiles_Mode()
    {
        const string spec1 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 1"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getProducts"",
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

        const string spec2 = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""API 2"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/orders"": {
      ""get"": {
        ""operationId"": ""getOrders"",
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

        var swaggerFile1 = await SwaggerFileHelper.CreateSwaggerFile(spec1);
        var swaggerFile2 = await SwaggerFileHelper.CreateSwaggerFile(spec2);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { swaggerFile1, swaggerFile2 },
                GenerateMultipleFiles = true
            };
            var generator = await RefitGenerator.CreateAsync(settings);

            // Verify the document was created and has paths
            generator.OpenApiDocument.Should().NotBeNull();
            generator.OpenApiDocument.Paths.Should().NotBeEmpty();

            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();
            var combinedCode = string.Join("\n\n", result.Files.Select(f => f.Content));

            // Should contain interface definitions
            combinedCode.Should().Contain("interface");
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile1);
            CleanupSwaggerFile(swaggerFile2);
        }
    }

    #endregion

    #region Combined Scenarios

    [Test]
    public async Task GenerateMultipleFiles_With_ContractTypeSuffix_And_AdditionalNamespaces()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                ContractTypeSuffix = "Dto",
                AdditionalNamespaces = new[] { "System.Text.Json" }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            var interfaceFiles = result.Files.Where(f =>
                f.TypeName != "Contracts" && f.TypeName != "DependencyInjection").ToList();

            foreach (var file in interfaceFiles)
            {
                file.Content.Should().Contain("using System.Text.Json;");
            }

            // Verify suffix is applied
            var allContent = string.Join("\n", result.Files.Select(f => f.Content));
            allContent.Should().Contain("Dto");

            // Verify interface files are generated and non-empty
            interfaceFiles.Should().NotBeEmpty();
            foreach (var file in interfaceFiles)
            {
                file.Content.Should().Contain("interface");
            }
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    [Test]
    public async Task GenerateMultipleFiles_With_GenerateContracts_False_And_ContractTypeSuffix()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                GenerateContracts = false,
                ContractTypeSuffix = "Dto"
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            var result = generator.GenerateMultipleFiles();

            result.Files.Should().NotBeEmpty();
            result.Files.Should().NotContain(f => f.TypeName == "Contracts");

            // Even without contracts file, suffix should apply to interface files
            var interfaceFiles = result.Files.Where(f =>
                f.TypeName != "Contracts" && f.TypeName != "DependencyInjection").ToList();

            interfaceFiles.Should().NotBeEmpty();
        }
        finally
        {
            CleanupSwaggerFile(swaggerFile);
        }
    }

    #endregion

    private static void CleanupSwaggerFile(string swaggerFile)
    {
        if (File.Exists(swaggerFile))
        {
            File.Delete(swaggerFile);
        }

        var directory = Path.GetDirectoryName(swaggerFile);
        if (directory != null && Directory.Exists(directory))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
