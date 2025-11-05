using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class InlineJsonConvertersTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Enum Test API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/pets/{petId}"": {
      ""get"": {
        ""summary"": ""Get pet by ID"",
        ""operationId"": ""getPetById"",
        ""parameters"": [
          {
            ""name"": ""petId"",
            ""in"": ""path"",
            ""required"": true,
            ""type"": ""integer""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""$ref"": ""#/definitions/Pet""
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
        },
        ""status"": {
          ""type"": ""string"",
          ""enum"": [
            ""available"",
            ""pending"",
            ""sold""
          ]
        }
      }
    }
  }
}
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Generated_Code_Contains_JsonConverter_By_Default()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: true);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter");
            generatedCode.Should().Contain("Status { get; set; }");
        }
    }

    [Fact]
    public async Task Generated_Code_Does_Not_Contain_JsonConverter_When_Disabled()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            generatedCode.Should().Contain("Status { get; set; }");
            generatedCode.Should().Contain("public enum PetStatus");
        }
    }

    [Fact]
    public async Task Generated_Code_Without_JsonConverter_Can_Build()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(bool inlineJsonConverters = true)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    InlineJsonConverters = inlineJsonConverters
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
