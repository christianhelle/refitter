## Description:

Describe the changes include in this pull request. What is this fixing or improving.

If this pull request resolves or implements an existing issue then please associate it.

#### Example OpenAPI Specifications:
```yaml
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
        "204":
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
```

#### Example generated Refit interface
```cs
public partial interface IReferenceparameters
{
	/// <summary>
	/// This method allows to remove an order item from an order, by specifying their ids.
	/// </summary>
	[Delete("/orders/{orderId}/order-items/{orderItemId}")]
	Task OrderItems();
}
```