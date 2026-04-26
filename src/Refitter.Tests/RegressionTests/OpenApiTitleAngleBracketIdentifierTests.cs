using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

public class OpenApiTitleAngleBracketIdentifierTests
{
    private const string OpenApiSpec = """
        openapi: '3.0.0'
        info:
          title: 'Invoice<Client> API'
          version: '1.0'
        paths:
          /invoices:
            get:
              operationId: 'GetInvoices'
              responses:
                '200':
                  description: 'ok'
                  content:
                    application/json:
                      schema:
                        type: array
                        items:
                          $ref: '#/components/schemas/Invoice'
        components:
          schemas:
            Invoice:
              type: object
              properties:
                id:
                  type: string
        """;

    [Test]
    public async Task OpenApi_Title_With_Angle_Brackets_Does_Not_Leak_Invalid_Interface_Or_Context_Names()
    {
        string generatedCode = await GenerateCode();

        generatedCode.Should().Contain("partial interface IInvoiceClientAPI");
        generatedCode.Should().Contain("internal partial class InvoiceClientAPISerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
        generatedCode.Should().NotContain("IInvoice<Client>API");
        generatedCode.Should().NotContain("Invoice<Client>APISerializerContext");
    }

    [Test]
    public async Task OpenApi_Title_With_Angle_Brackets_Generated_Code_Builds()
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
            GenerateJsonSerializerContext = true,
            Naming = new NamingSettings
            {
                UseOpenApiTitle = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
