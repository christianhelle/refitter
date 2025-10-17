using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataArrayTests
{
    private const string OpenApiSpec = @"
{
    ""openapi"": ""3.0.2"",
    ""paths"": {
        ""/foo/{id}/files"": {
            ""post"": {
                ""summary"": ""uploads multiple files"",
                ""operationId"": ""uploadFiles"",
                ""parameters"": [
                    {
                        ""name"": ""id"",
                        ""in"": ""path"",
                        ""description"": ""Id of the foo resource"",
                        ""required"": true,
                        ""schema"": {
                            ""type"": ""integer"",
                            ""format"": ""int64""
                        }
                    }
                ],
                ""requestBody"": {
                    ""content"": {
                        ""multipart/form-data"": {
                            ""schema"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""files"": {
                                        ""type"": ""array"",
                                        ""items"": {
                                            ""type"": ""string"",
                                            ""format"": ""binary""
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                ""responses"": {
                    ""200"": {
                        ""description"": ""successful operation""
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

    [Fact]
    public async Task Generated_Code_Contains_MultiPart_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Multipart]");
        generatedCode.Should().NotContain("Content-Type: multipart/form-data");
    }

    [Fact]
    public async Task Generated_Code_Uses_IEnumerable_StreamPart_For_File_Array()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("long id, IEnumerable<StreamPart> files");
        generatedCode.Should().NotContain("FileParameter");
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
