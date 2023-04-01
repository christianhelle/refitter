using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests;

public class PathParametersWithUrlTests
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
        var generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        var generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerUrl = "https://petstore.swagger.io/v2/swagger.json";
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerUrl };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }


}