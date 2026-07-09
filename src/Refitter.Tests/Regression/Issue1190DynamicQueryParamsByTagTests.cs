using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Regression;

/// <summary>
/// Regression tests for Issue #1190: dynamic QueryParams types are incorrectly shared
/// by GET endpoints with the same method name across different controllers/tags.
/// </summary>
public class Issue1190DynamicQueryParamsByTagTests
{
    private const string OpenApiSpec = """
        openapi: '3.0.0'
        info:
          title: 'Issue 1190 API'
          version: '1.0'
        paths:
          /MachineTool/Page:
            get:
              tags:
                - 'MachineTool'
              operationId: 'Page'
              parameters:
                - in: query
                  name: machineToolParam
                  schema:
                    type: string
                - in: query
                  name: machineToolFilter
                  schema:
                    type: string
              responses:
                '200':
                  description: 'ok'
          /Material/Page:
            get:
              tags:
                - 'Material'
              operationId: 'Page'
              parameters:
                - in: query
                  name: materialParam
                  schema:
                    type: string
                - in: query
                  name: materialFilter
                  schema:
                    type: string
              responses:
                '200':
                  description: 'ok'
        """;

    [Test]
    public async Task ByTag_Generates_Distinct_QueryParams_Types_Per_Tag()
    {
        string generatedCode = await GenerateCode();

        generatedCode.Should().Contain("partial interface IMachineToolApi");
        generatedCode.Should().Contain("partial interface IMaterialApi");

        generatedCode.Should().Contain("[Query] MachineToolPageAsyncQueryParams queryParams");
        generatedCode.Should().Contain("[Query] MaterialPageAsyncQueryParams queryParams");

        generatedCode.Should().Contain("public class MachineToolPageAsyncQueryParams");
        generatedCode.Should().Contain("public class MaterialPageAsyncQueryParams");

        generatedCode.Should().Contain("MachineToolParam");
        generatedCode.Should().Contain("MachineToolFilter");
        generatedCode.Should().Contain("MaterialParam");
        generatedCode.Should().Contain("MaterialFilter");
    }

    [Category("Integration")]
    [Test]
    public async Task ByTag_Generated_Code_With_Distinct_QueryParams_Builds()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            UseDynamicQuerystringParameters = true,
            OperationNameTemplate = "{operationName}Async"
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
