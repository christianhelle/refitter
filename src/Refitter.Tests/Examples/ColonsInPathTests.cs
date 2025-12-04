using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ColonsInPathTests
{
    private const string OpenApiSpec = @"
swagger: '2.0'
info:
  title: Reference parameters
  version: v0.0.1
paths:
  '/orders/{orderId}/:orderItems/{orderItemId}':
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

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_OperationName_Without_Colons()
    {
        var generatedCode = await GenerateCode();
        using var scope = new AssertionScope();
        generatedCode.Should().NotContain(":OrderItems(");
        generatedCode.Should().Contain("\"/orders/{orderId}/:orderItems/{orderItemId}\"");
    }

    [Test]
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
