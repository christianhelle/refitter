using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class FormDataParameterCasingTests
{
    private const string OpenApiSpec =
        @"
{
  ""swagger"": ""2.0"",
  ""paths"": {
    ""/endpoint"": {
      ""post"": {
        ""consumes"": [""multipart/form-data""],
        ""parameters"": [
          {
            ""in"": ""formData"",
            ""name"": ""PascalCase"",
            ""type"": ""integer"",
            ""required"": true
          },
          {
            ""in"": ""formData"",
            ""name"": ""anotherField"",
            ""type"": ""string"",
            ""required"": false
          },
          {
            ""in"": ""formData"",
            ""name"": ""file"",
            ""type"": ""file"",
            ""required"": false
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""$ref"": ""#/definitions/MyResponse""
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""MyResponse"": {
      ""type"": ""object"",
      ""properties"": {
        ""id"": {
          ""type"": ""integer""
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
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Fact]
    public async Task Generated_Code_Contains_Multipart_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Multipart]");
    }

    [Fact]
    public async Task FormData_Parameters_Should_Have_AliasAs_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[AliasAs(\"PascalCase\")] int pascalCase");
        generatedCode.Should().NotContain("[AliasAs(\"anotherField\")]");
        generatedCode.Should().NotContain("[AliasAs(\"AnotherField\")]");
        generatedCode.Should().Contain("string anotherField");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
