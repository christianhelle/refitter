using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class AnyTypeBodySerializationTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Example API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/some/endpoint"": {
      ""post"": {
        ""summary"": ""Create message with untyped payload"",
        ""operationId"": ""createMessage"",
        ""requestBody"": {
          ""required"": true,
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""type"": ""object"",
                ""additionalProperties"": true
              }
            }
          }
        },
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object"",
                  ""additionalProperties"": true
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

    [Fact]
    public async Task Generated_Code_Contains_BodySerializationMethod_For_Object_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Body(BodySerializationMethod.Serialized)] object body");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "petstore.json"
        };

        return await RefitGenerator.Generate(
            new OpenApiSpecification(OpenApiSpec),
            settings
        );
    }
}
