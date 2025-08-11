using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class OptionalNullableParametersTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /job/{Id}:
    post:
      tags:
      - 'Jobs'
      operationId: 'Update job details'
      description: 'Update the details of the specified job.'
      parameters:
        - in: 'path'
          name: 'Id'
          description: 'Foo Id'
          required: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Title'
          description: 'Job title'
          nullable: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Description'
          description: 'Job description'
          optional: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Contact'
          description: 'Contact Person'
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }
    
    [Fact]
    public async Task Generates_Nullable_Directive()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("#nullable");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("contact")]
    [InlineData("description")]
    public async Task Generates_Nullable_Parameters(string parameterName)
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain($"string? {parameterName} = default");
    }

    [Fact]
    public async Task Generates_CancellationToken_Last()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("CancellationToken cancellationToken = default);");
    }

    [Fact]
    public async Task Generates_DynamicQuerystring_Param()
    {
        string generateCode = await GenerateCode(true);
        generateCode.Should().Contain("string id, [Query] UpdateJobDetailsQueryParams? queryParams = default, CancellationToken cancellationToken = default);");
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

    private static async Task<string> GenerateCode(bool useDynamicQuerystringParameters = false)
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseCancellationTokens = true,
            OptionalParameters = true,
            UseDynamicQuerystringParameters = useDynamicQuerystringParameters,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
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