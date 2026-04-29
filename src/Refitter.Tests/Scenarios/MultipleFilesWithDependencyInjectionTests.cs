using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class MultipleFilesWithDependencyInjectionTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Multi-File API"",
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

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedFiles = await GenerateCode();
        generatedFiles.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        var generatedFiles = await GenerateCode();
        var combinedCode = string.Join("\n\n", generatedFiles.Select(f => f.Content));
        BuildHelper
            .BuildCSharp(combinedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_DI_Registration()
    {
        var generatedFiles = await GenerateCode();
        var combinedCode = string.Join("\n\n", generatedFiles.Select(f => f.Content));
        combinedCode.Should().Contain("AddRefitClient");
        combinedCode.Should().Contain("IServiceCollection");
    }

    [Test]
    public async Task Generates_Multiple_Files()
    {
        var generatedFiles = await GenerateCode();
        generatedFiles.Should().HaveCountGreaterThan(1);
    }

    [Test]
    public async Task Generated_Files_Include_Contracts_And_Interface()
    {
        var generatedFiles = await GenerateCode();
        var fileNames = generatedFiles.Select(f => f.Filename).ToList();

        fileNames.Should().Contain(x => x.Contains("Contracts"));
        fileNames.Should().Contain(x => x.EndsWith(".cs") && !x.Contains("Contracts"));
    }

    private static async Task<IReadOnlyList<GeneratedCode>> GenerateCode()
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
            return generator.GenerateMultipleFiles().Files;
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
