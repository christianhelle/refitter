using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class CollectionFormatTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Collection Format API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/test"": {
      ""get"": {
        ""summary"": ""Test endpoint with array parameters"",
        ""description"": ""Endpoint to test different collection formats"",
        ""operationId"": ""getWithArrayParameter"",
        ""parameters"": [
          {
            ""name"": ""ids"",
            ""in"": ""query"",
            ""description"": ""Array of IDs"",
            ""required"": true,
            ""type"": ""array"",
            ""items"": {
              ""type"": ""integer"",
              ""format"": ""int32""
            }
          },
          {
            ""name"": ""tags"",
            ""in"": ""query"",
            ""description"": ""Array of tags"",
            ""required"": false,
            ""type"": ""array"",
            ""items"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success response""
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
    [Arguments(CollectionFormat.Multi, "Query(CollectionFormat.Multi)")]
    [Arguments(CollectionFormat.Csv, "Query(CollectionFormat.Csv)")]
    [Arguments(CollectionFormat.Ssv, "Query(CollectionFormat.Ssv)")]
    [Arguments(CollectionFormat.Tsv, "Query(CollectionFormat.Tsv)")]
    [Arguments(CollectionFormat.Pipes, "Query(CollectionFormat.Pipes)")]
    public async Task Generated_Code_Contains_Expected_Collection_Format(CollectionFormat format, string expectedAttribute)
    {
        string generatedCode = await GenerateCode(format);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain(expectedAttribute);
            // Check both array parameters use the same format
            generatedCode.Should().Contain($"[{expectedAttribute}] IEnumerable<int> ids");
            generatedCode.Should().Contain($"[{expectedAttribute}] IEnumerable<string> tags");
        }
    }

    private static async Task<string> GenerateCode(CollectionFormat format = CollectionFormat.Multi)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                CollectionFormat = format
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
