using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class DuplicateOperationIdTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Duplicate OperationId API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/items"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""summary"": ""Get items"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    },
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""summary"": ""Get products (duplicate operationId)"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    },
    ""/api/orders"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""summary"": ""Get orders (duplicate operationId)"",
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

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Handles_Duplicate_OperationIds_Gracefully()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_Methods()
    {
        string generatedCode = await GenerateCode();
        // When operationIds are duplicated, NSwag falls back to path-based method names
        generatedCode.Should().Contain("Items");
        generatedCode.Should().Contain("Products");
        generatedCode.Should().Contain("Orders");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile
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
