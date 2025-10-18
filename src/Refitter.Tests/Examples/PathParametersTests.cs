using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class PathParametersTests
{
    private const string OpenApiSpec = @"
swagger: '2.0'
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
    public async Task Generates_Path_Parameters()
    {
        var generatedCode = await GenerateCode();
        using var scope = new AssertionScope();
        generatedCode.Should().Contain("Guid orderId");
        generatedCode.Should().Contain("Guid orderItemId");
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

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
