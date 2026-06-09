using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Regression;

public class MissingInterfaceXmlDocsWarningsAsErrorsTests
{
    private const string ByTagSpec = """
        openapi: '3.0.0'
        info:
          title: 'Issue 1109 Repro'
          version: '1.0'
        paths:
          /users:
            get:
              tags:
                - 'Users'
              operationId: 'GetUsers'
              responses:
                '200':
                  description: 'ok'
          /orders:
            get:
              tags:
                - 'Orders'
              operationId: 'GetOrders'
              responses:
                '200':
                  description: 'ok'
        """;

    private const string ByEndpointSpec = """
        openapi: '3.0.0'
        info:
          title: 'Issue 1109 Endpoint Repro'
          version: '1.0'
        paths:
          /users:
            get:
              operationId: 'GetUsers'
              responses:
                '200':
                  description: 'ok'
          /orders:
            get:
              responses:
                '200':
                  description: 'ok'
        """;

    [Test]
    public async Task ByTag_Generates_Interface_Fallback_Summary_When_Tag_Description_Is_Missing()
    {
        string generatedCode = await GenerateCode(ByTagSpec, MultipleInterfaces.ByTag);

        generatedCode.Should().Contain("/// <summary>Operations for Users.</summary>");
        generatedCode.Should().Contain("/// <summary>Operations for Orders.</summary>");
    }

    [Test]
    public async Task ByTag_Generated_Code_Builds_With_Warnings_As_Errors()
    {
        string generatedCode = await GenerateCode(ByTagSpec, MultipleInterfaces.ByTag);

        BuildHelper.BuildCSharp(warningsAsErrors: true, generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task ByEndpoint_Generates_Interface_Fallback_Summary_When_Endpoint_Summary_Is_Missing()
    {
        string generatedCode = await GenerateCode(ByEndpointSpec, MultipleInterfaces.ByEndpoint);

        generatedCode.Should().Contain("/// <summary>Operations for GetUsers.</summary>");
        generatedCode.Should().Contain("partial interface IOrdersEndpoint");
        generatedCode.Should().Contain("/// <summary>Operations for");
    }

    [Test]
    public async Task ByEndpoint_Generated_Code_Builds_With_Warnings_As_Errors()
    {
        string generatedCode = await GenerateCode(ByEndpointSpec, MultipleInterfaces.ByEndpoint);

        BuildHelper.BuildCSharp(warningsAsErrors: true, generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec, MultipleInterfaces multipleInterfaces)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = multipleInterfaces
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
