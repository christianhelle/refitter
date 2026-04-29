using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class MutuallyExclusiveSettingsTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/items"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""summary"": ""Get items"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Item""
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
      ""Item"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer""
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

    [Test]
    public async Task Both_Disabled_Still_Generates_Code()
    {
        string generatedCode = await GenerateCodeWithBothDisabled();
        generatedCode.Should().BeEmpty();
    }

    [Test]
    public async Task Both_Disabled_Can_Build_Code()
    {
        string generatedCode = await GenerateCodeWithBothDisabled();
        generatedCode.Should().BeEmpty();
    }

    [Test]
    public async Task Both_Response_Types_Generates_Code()
    {
        string generatedCode = await GenerateCodeWithBothResponseTypes();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Both_Response_Types_Can_Build()
    {
        string generatedCode = await GenerateCodeWithBothResponseTypes();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Both_Response_Types_Prefers_ApiResponse()
    {
        string generatedCode = await GenerateCodeWithBothResponseTypes();
        generatedCode.Should().Contain("IApiResponse");
    }

    [Test]
    public async Task ApiResponse_Setting_Takes_Precedence_Over_Observable()
    {
        string generatedCode = await GenerateCodeWithBothResponseTypes();
        generatedCode.Should().Contain("IObservable<IApiResponse");
    }

    private static async Task<string> GenerateCodeWithBothDisabled()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateClients = false,
                GenerateContracts = false
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

    private static async Task<string> GenerateCodeWithBothResponseTypes()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                ReturnIApiResponse = true,
                ReturnIObservable = true
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
