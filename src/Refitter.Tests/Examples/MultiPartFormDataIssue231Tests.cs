using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataIssue231Tests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",  
  ""paths"": {
    ""/animals"": {
      ""post"": {
        ""tags"": [
          ""Animals""
        ],
        ""requestBody"": {
          ""content"": {
            ""multipart/form-data"": {
              ""schema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""Name"": {
                    ""type"": ""string""
                  },
                  ""Description"": {
                    ""type"": ""string""
                  },
                  ""AnimalClassFile"": {
                    ""type"": ""string"",
                    ""format"": ""binary""
                  }
                }
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""description"": ""Created""
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
    public async Task Generated_Code_Contains_Correct_Parameter_Names()
    {
        string generatedCode = await GenerateCode();
        // Name should be converted to name (camelCase) with AliasAs
        generatedCode.Should().Contain("[AliasAs(\"Name\")] string name");
        // Description should be converted to description (camelCase) with AliasAs
        generatedCode.Should().Contain("[AliasAs(\"Description\")] string description");
        // File parameter
        generatedCode.Should().Contain("[AliasAs(\"AnimalClassFile\")] StreamPart animalClassFile");
    }

    [Test]
    public async Task Generated_Code_Has_All_Three_Parameters()
    {
        string generatedCode = await GenerateCode();
        // Should have Name, Description, and AnimalClassFile parameters
        generatedCode.Should().Contain("string name");
        generatedCode.Should().Contain("string description");
        generatedCode.Should().Contain("StreamPart animalClassFile");
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
