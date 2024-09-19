using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataTests
{
    private const string OpenApiSpec = @"
{
   ""openapi"":""3.0.2"",
   ""paths"":{
      ""/foo/{id}/files"":{
         ""post"":{
            ""summary"":""uploads a file"",
            ""operationId"":""uploadFile"",
            ""parameters"":[
               {
                  ""name"":""id"",
                  ""in"":""path"",
                  ""description"":""Id of the foo resource"",
                  ""required"":true,
                  ""schema"":{
                     ""type"":""integer"",
                     ""format"":""int64""
                  }
               }
            ],
            ""requestBody"":{
               ""content"":{
                  ""multipart/form-data"":{
                     ""schema"":{
                        ""type"":""object"",
                        ""properties"":{
                           ""formFile"":{
                              ""type"":""string"",
                              ""format"":""binary""
                           }
                        }
                     },
                     ""encoding"":{
                        ""formFile"":{
                           ""style"":""form""
                        }
                     }
                  }
               }
            },
            ""responses"":{
               ""200"":{
                  ""description"":""successful operation""
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

    [Fact]
    public async Task Generated_Code_Contains_MultiPart_Attribute()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("[Multipart]");
    }

    [Fact]
    public async Task Generated_Code_Contains_StreamPart_Parameter()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("StreamPart");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
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