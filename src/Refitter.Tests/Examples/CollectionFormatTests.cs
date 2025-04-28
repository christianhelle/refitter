using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

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

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(CollectionFormat.Multi, "Query(CollectionFormat.Multi)")]
    [InlineData(CollectionFormat.Csv, "Query(CollectionFormat.Csv)")]
    [InlineData(CollectionFormat.Ssv, "Query(CollectionFormat.Ssv)")]
    [InlineData(CollectionFormat.Tsv, "Query(CollectionFormat.Tsv)")]
    [InlineData(CollectionFormat.Pipes, "Query(CollectionFormat.Pipes)")]
    public async Task Generated_Code_Contains_Expected_Collection_Format(CollectionFormat format, string expectedAttribute)
    {
        string generateCode = await GenerateCode(format);
        
        using (new AssertionScope())
        {
            generateCode.Should().Contain(expectedAttribute);
            // Check both array parameters use the same format
            generateCode.Should().Contain($"[{expectedAttribute}] IEnumerable<int> ids");
            generateCode.Should().Contain($"[{expectedAttribute}] IEnumerable<string> tags");
        }
    }

    private static async Task<string> GenerateCode(CollectionFormat format = CollectionFormat.Multi)
    {
        string swaggerFile = await CreateSwaggerFile(OpenApiSpec);
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

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.json";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}