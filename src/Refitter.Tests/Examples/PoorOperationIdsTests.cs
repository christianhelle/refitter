using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class PoorOperationIdsTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"" : ""3.0.1"",
  ""paths"" : {
    ""/api/v1/{id}/foo"" : {
      ""get"" : {
        ""tags"" : [ ""foo"" ],
        ""operationId"" : ""get-/api/v1/{id}/foo_getAll"",
        ""parameters"" : [ {
          ""name"" : ""id"",
          ""in"" : ""path"",
          ""required"" : true,
          ""style"" : ""simple"",
          ""explode"" : false,
          ""schema"" : {
            ""type"" : ""integer"",
            ""format"" : ""int32""
          }
        } ],
        ""responses"" : {
          ""200"" : {
            ""description"" : ""successful operation""
          }
        }
      }
    },
    ""/api/v1/{id}/bar"" : {
      ""get"" : {
        ""tags"" : [ ""bar"" ],
        ""operationId"" : ""get-/api/v1/{id}/bar_getAll"",
        ""parameters"" : [ {
          ""name"" : ""id"",
          ""in"" : ""path"",
          ""required"" : true,
          ""style"" : ""simple"",
          ""explode"" : false,
          ""schema"" : {
            ""type"" : ""integer"",
            ""format"" : ""int32""
          }
        } ],
        ""responses"" : {
          ""200"" : {
            ""description"" : ""successful operation""
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

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var foo = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(foo);
        var generateCode = sut.Generate();
        return generateCode;
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
