using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
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

    [TestMethod]
    public async Task Generated_Code_Contains_BodySerializationMethod_For_Object_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Body(BodySerializationMethod.Serialized)] object body");
    }

    [TestCategory("Integration")]
    [TestMethod]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
