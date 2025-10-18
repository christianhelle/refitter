using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class NoContentApiResponseTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Reference parameters
  version: v0.0.1
paths:
  '/orders/{orderId}/order-items/{orderItemId}':
    parameters:
      - $ref: '#/parameters/OrderId'
      - $ref: '#/parameters/OrderItemId'
    delete:
      summary: Delete an order item
      description: >-
        This method allows to remove an order item from an order, by specifying
        their ids.
      responses:
        '204':
          description: No Content.
        default:
          description: Default response
          schema:
            $ref: '#/definitions/Error'
definitions:
  Error:
    type: object
    properties:
      message:
          type: string
parameters:
  OrderId:
    name: orderId
    in: path
    description: Identifier of the order.
    required: true
    type: string
    format: uuid
  OrderItemId:
    name: orderItemId
    in: path
    description: Identifier of the order item.
    required: true
    type: string
    format: uuid
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generates_IApiResponse_For_NoContent()
    {
        var generatedCode = await GenerateCode();
        using var scope = new AssertionScope();
        generatedCode.Should().Contain("Task<IApiResponse>");
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

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        settings.ReturnIApiResponse = true;

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
