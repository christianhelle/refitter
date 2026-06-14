using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitSchemaCleanerTests
{
    [Test]
    public async Task Clean_WithRemoveUnusedSchemaFalse_ReturnsSameDocument()
    {
        var spec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /items:
    get:
      operationId: GetItems
      responses:
        '200':
          description: Success
components:
  schemas:
    UsedItem:
      type: object
      properties:
        id: { type: string }
    UnusedItem:
      type: object
      properties:
        ignored: { type: string }
";
        var json = await OpenApiYamlDocument.FromYamlAsync(spec);
        var document = await OpenApiDocument.FromJsonAsync(json.ToJson());
        var sut = new RefitSchemaCleaner();

        var result = sut.Clean(document, false, [], false);

        result.Components.Schemas.Count.Should().Be(2);
    }

    [Test]
    public async Task Clean_WithRemoveUnusedSchemaTrue_RemovesUnusedSchemas()
    {
        var spec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /items:
    get:
      operationId: GetItems
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UsedItem'
components:
  schemas:
    UsedItem:
      type: object
      properties:
        id: { type: string }
    UnusedItem:
      type: object
      properties:
        ignored: { type: string }
";
        var json = await OpenApiYamlDocument.FromYamlAsync(spec);
        var document = await OpenApiDocument.FromJsonAsync(json.ToJson());
        var sut = new RefitSchemaCleaner();

        var result = sut.Clean(document, true, [], false);

        result.Components.Schemas.Should().ContainKey("UsedItem");
        result.Components.Schemas.Should().NotContainKey("UnusedItem");
    }

    [Test]
    public async Task Clean_DoesNotMutateOriginalDocument()
    {
        var spec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /items:
    get:
      operationId: GetItems
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UsedItem'
components:
  schemas:
    UsedItem:
      type: object
      properties:
        id: { type: string }
    UnusedItem:
      type: object
      properties:
        ignored: { type: string }
";
        var json = await OpenApiYamlDocument.FromYamlAsync(spec);
        var document = await OpenApiDocument.FromJsonAsync(json.ToJson());
        var originalCount = document.Components.Schemas.Count;
        var sut = new RefitSchemaCleaner();

        sut.Clean(document, true, [], false);

        document.Components.Schemas.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task Clean_KeepsSchemasMatchingKeepPattern()
    {
        var spec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /items:
    get:
      operationId: GetItems
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/UsedItem'
components:
  schemas:
    UsedItem:
      type: object
      properties:
        id: { type: string }
    KeepItem:
      type: object
      properties:
        name: { type: string }
    UnusedItem:
      type: object
      properties:
        ignored: { type: string }
";
        var json = await OpenApiYamlDocument.FromYamlAsync(spec);
        var document = await OpenApiDocument.FromJsonAsync(json.ToJson());
        var sut = new RefitSchemaCleaner();

        var result = sut.Clean(document, true, ["Keep.*"], false);

        result.Components.Schemas.Should().ContainKey("UsedItem");
        result.Components.Schemas.Should().ContainKey("KeepItem");
        result.Components.Schemas.Should().NotContainKey("UnusedItem");
    }
}
