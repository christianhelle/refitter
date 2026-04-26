using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

public class ByEndpointQueryParamsInternalITests
{
    private const string OpenApiSpec = """
        openapi: '3.0.0'
        info:
          title: 'Invoice API'
          version: '1.0'
        paths:
          /invoice-info/{id}:
            get:
              operationId: 'GetInvoiceInfo'
              parameters:
                - in: path
                  name: id
                  required: true
                  schema:
                    type: string
                - in: query
                  name: includeItems
                  required: true
                  schema:
                    type: string
                - in: query
                  name: itemStatus
                  required: true
                  schema:
                    type: string
              responses:
                '200':
                  description: 'ok'
        """;

    [Test]
    public async Task ByEndpoint_Dynamic_Query_Params_Keep_Internal_I_Characters()
    {
        string generatedCode = await GenerateCode();

        generatedCode.Should().Contain("partial interface IGetInvoiceInfoEndpoint");
        generatedCode.Should().Contain("[Query] GetInvoiceInfoQueryParams queryParams");
        generatedCode.Should().Contain("public record GetInvoiceInfoQueryParams");
        generatedCode.Should().NotContain("GetnvoicenfoQueryParams");
    }

    [Test]
    public async Task ByEndpoint_Dynamic_Query_Params_With_Internal_I_Characters_Build()
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
            MultipleInterfaces = MultipleInterfaces.ByEndpoint,
            UseDynamicQuerystringParameters = true,
            ImmutableRecords = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
