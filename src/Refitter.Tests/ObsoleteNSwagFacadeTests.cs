// Covers the obsolete NSwag-typed public API until it is removed
#pragma warning disable CS0618
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class ObsoleteNSwagFacadeTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Facade API'
paths:
  /pets:
    get:
      tags: ['Pets']
      operationId: 'getPets'
      responses:
        '200':
          description: 'Pets'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Pet'
  /owners:
    get:
      tags: ['Owners']
      operationId: 'getOwners'
      responses:
        '200':
          description: 'Owners'
components:
  schemas:
    Pet:
      type: 'object'
    Unused:
      type: 'object'
";

    private const string OtherOpenApiSpec = @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Other Facade API'
paths:
  /stores:
    get:
      operationId: 'getStores'
      responses:
        '200':
          description: 'Stores'
";

    [Test]
    public async Task OpenApiDocumentFactory_Loads_Document()
    {
        var path = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var otherPath = await SwaggerFileHelper.CreateSwaggerFile(OtherOpenApiSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(path);
        var merged = await OpenApiDocumentFactory.CreateAsync([path, otherPath]);

        document.Paths.Keys.Should().BeEquivalentTo("/pets", "/owners");
        merged.Paths.Keys.Should().BeEquivalentTo("/pets", "/owners", "/stores");
    }

    [Test]
    public async Task RefitDocumentFilter_Filters_Document()
    {
        var document = await OpenApiDocumentFactory.CreateAsync(await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec));

        RefitDocumentFilter.FilterByTags(document, ["Pets"]).Paths.Keys.Should().BeEquivalentTo("/pets");
        RefitDocumentFilter.FilterByPath(document, ["^/owners"]).Paths.Keys.Should().BeEquivalentTo("/owners");
    }

    [Test]
    public async Task SchemaCleaner_Removes_Unreferenced_Schema()
    {
        var document = await OpenApiDocumentFactory.CreateAsync(await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec));

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Keys.Should().BeEquivalentTo("Pet");
    }
}
