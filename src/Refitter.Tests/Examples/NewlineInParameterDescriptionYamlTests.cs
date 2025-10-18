using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class NewlineInParameterDescriptionYamlTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Newline in parameter description test
  version: v1.0.0
paths:
  /dialogs/search:
    get:
      summary: Search dialogs
      operationId: SearchDialogs
      parameters:
        - name: deleted
          in: query
          description: |
            If set to 'include', the result will include both deleted and non-deleted dialogs
            If set to 'exclude', the result will only include non-deleted dialogs
            If set to 'only', the result will only include deleted dialogs
          schema:
            type: string
            enum:
              - include
              - exclude
              - only
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  type: object
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Newlines_In_Parameter_Description_Are_Properly_Formatted()
    {
        var generatedCode = await GenerateCode();

        generatedCode.Should().Contain("/// If set to 'include', the result will include both deleted and non-deleted dialogs");
        generatedCode.Should().Contain("/// If set to 'exclude', the result will only include non-deleted dialogs");
        generatedCode.Should().Contain("/// If set to 'only', the result will only include deleted dialogs");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Newlines_In_QueryParams_Class_Description_Are_Properly_Formatted()
    {
        var generatedCode = await GenerateCodeWithDynamicQuerystring();

        // Each line of the description should be prefixed with ///
        generatedCode.Should().Contain("/// If set to 'include', the result will include both deleted and non-deleted dialogs");
        generatedCode.Should().Contain("/// If set to 'exclude', the result will only include non-deleted dialogs");
        generatedCode.Should().Contain("/// If set to 'only', the result will only include deleted dialogs");
    }

    [Fact]
    public async Task Can_Build_Generated_Code_With_DynamicQuerystring()
    {
        var generatedCode = await GenerateCodeWithDynamicQuerystring();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> GenerateCodeWithDynamicQuerystring()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
