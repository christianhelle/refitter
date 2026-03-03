using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataIssue222Tests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",
  ""paths"": {
    ""/upload"": {
      ""post"": {
        ""requestBody"": {
          ""content"": {
            ""multipart/form-data"": {
              ""schema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""OrganizationalUnitIds"": {
                    ""type"": ""array"",
                    ""items"": {
                      ""type"": ""integer"",
                      ""format"": ""int64""
                    }
                  },
                  ""file"": {
                    ""type"": ""string"",
                    ""format"": ""binary""
                  }
                }
              }
            }
          }
        },
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
    public async Task Generated_Code_Contains_Multipart_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Multipart]");
    }

    [Test]
    public async Task Generated_Code_Contains_OrganizationalUnitIds_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("OrganizationalUnitIds");
        generatedCode.Should().Contain("organizationalUnitIds");
    }

    [Test]
    public async Task Generated_Code_Contains_File_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("StreamPart file");
    }

    [Test]
    public async Task Generated_Code_Contains_Correct_Parameter_Count()
    {
        string generatedCode = await GenerateCode();
        // Should have both OrganizationalUnitIds and file parameters
        generatedCode.Should().Contain("[AliasAs(\"OrganizationalUnitIds\")]");
        generatedCode.Should().Contain("file");
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
