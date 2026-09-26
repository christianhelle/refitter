using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class DocumentInfoTests
{
    private const string OpenApiV3Spec = @"
openapi: '3.0.0'
info:
  version: '2.1.0'
  title: 'Document Info API'
paths:
  /pets:
    get:
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
      operationId: 'getOwners'
      responses:
        '200':
          description: 'Owners'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Owner'
components:
  schemas:
    Pet:
      type: 'object'
    Owner:
      type: 'object'
";

    private const string SwaggerV2Spec = @"
swagger: '2.0'
info:
  version: '1.0.0'
  title: 'Swagger Document Info API'
paths:
  /pets:
    get:
      operationId: 'getPets'
      responses:
        '200':
          description: 'Pets'
          schema:
            $ref: '#/definitions/Pet'
definitions:
  Pet:
    type: 'object'
";

    [Test]
    public async Task Describes_OpenApi_V3_Document()
    {
        var generator = await CreateGenerator(OpenApiV3Spec, new RefitGeneratorSettings());

        generator.DocumentInfo.Title.Should().Be("Document Info API");
        generator.DocumentInfo.Version.Should().Be("2.1.0");
        generator.DocumentInfo.Paths.Should().BeEquivalentTo("/pets", "/owners");
        generator.DocumentInfo.SchemaNames.Should().BeEquivalentTo("Pet", "Owner");
    }

    [Test]
    public async Task Describes_Swagger_V2_Document()
    {
        var generator = await CreateGenerator(SwaggerV2Spec, new RefitGeneratorSettings());

        generator.DocumentInfo.Title.Should().Be("Swagger Document Info API");
        generator.DocumentInfo.Paths.Should().BeEquivalentTo("/pets");
        generator.DocumentInfo.SchemaNames.Should().BeEquivalentTo("Pet");
    }

    [Test]
    public async Task Reflects_Filtered_And_Trimmed_Document()
    {
        var generator = await CreateGenerator(
            OpenApiV3Spec,
            new RefitGeneratorSettings { IncludePathMatches = ["^/pets"], TrimUnusedSchema = true });

        generator.DocumentInfo.Paths.Should().BeEquivalentTo("/pets");
        generator.DocumentInfo.SchemaNames.Should().BeEquivalentTo("Pet");
    }

    [Test]
    public async Task Obsolete_OpenApiDocument_Still_Returns_Generated_Document()
    {
        var generator = await CreateGenerator(OpenApiV3Spec, new RefitGeneratorSettings());

#pragma warning disable CS0618 // Covers the obsolete NSwag-typed property until it is removed
        generator.OpenApiDocument.Should().BeSameAs(generator.NSwagDocument);
#pragma warning restore CS0618
    }

    private static async Task<RefitGenerator> CreateGenerator(string spec, RefitGeneratorSettings settings)
    {
        settings.OpenApiPath = await SwaggerFileHelper.CreateSwaggerFile(spec);
        return await RefitGenerator.CreateAsync(settings);
    }
}
