using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class SystemPropertyInContractTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""3.0"",
  ""info"": {
    ""title"": ""XX"",
    ""version"": ""0.0.0""
  },
  ""host"": ""x.io"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""definitions"": {
    ""Dummy"": {
      ""type"": ""object"",
      ""properties"": {
        ""system"": {
          ""type"": ""string"",
          ""description"": ""XX""
        }
      },
      ""description"": ""XXX"",
      ""example"": {
        ""system"": ""Dummy""
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

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
