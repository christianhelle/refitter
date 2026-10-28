---
name: Pull request
title: ''
labels: enhancement
assignees: christianhelle

---

## Description:

Describe the changes include in this pull request. What is this fixing or improving.

If this pull request resolves or implements an existing issue then please associate it.

#### Checklist:

See the [Definition of Done](https://github.com/christianhelle/refitter/blob/main/CONTRIBUTING.md#definition-of-done) for details. Tick the items that apply to your change. If you would like help with any of these, just say so in the description.

- [ ] Related issue linked (`Closes #...`), if there is one
- [ ] New features: scenario tests that assert on the generated code and compile it with `BuildHelper.BuildCSharp()` (`[TestCategory("Integration")]`)
- [ ] Bug fixes: a regression test that fails without the fix
- [ ] Full test suite passes locally (`dotnet test --solution src/Refitter.slnx -c Release`)
- [ ] Documentation updated if settings or CLI arguments changed (`README.md`, `refitter-file-format.md`, `json-schema.json`)
- [ ] "Allow edits by maintainers" is enabled

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
