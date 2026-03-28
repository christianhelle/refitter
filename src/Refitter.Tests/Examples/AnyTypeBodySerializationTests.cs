using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

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

    [Test]
    public async Task Generated_Code_Contains_BodySerializationMethod_For_Object_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Body(BodySerializationMethod.Serialized)] object body");
    }

    [Test]
    public async Task Generated_Code_Contains_Json_BodySerializationMethod_When_Configured()
    {
        string generatedCode = await GenerateCode(BodySerializationMethod.Json);
        generatedCode.Should().Contain("[Body(BodySerializationMethod.Json)] object body");
    }

    [Test]
    public async Task Generated_Code_Contains_UrlEncoded_BodySerializationMethod_When_Configured()
    {
        string generatedCode = await GenerateCode(BodySerializationMethod.UrlEncoded);
        generatedCode.Should().Contain("[Body(BodySerializationMethod.UrlEncoded)] object body");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Json_BodySerializationMethod()
    {
        string generatedCode = await GenerateCode(BodySerializationMethod.Json);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        BodySerializationMethod method = BodySerializationMethod.Serialized)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            AnyTypeBodySerializationMethod = method,
        };

        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
