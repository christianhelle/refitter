using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class SchemaCleanerTests
{
    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task RemoveUnreferencedSchema_Removes_Unused_Schemas(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var initialCount = document.Components.Schemas.Count;

        var cleaner = new SchemaCleaner(document, []);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Count.Should().BeLessThanOrEqualTo(initialCount);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task RemoveUnreferencedSchema_Keeps_Referenced_Schemas(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, []);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Should().NotBeEmpty();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "Pet.*")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "Pet")]
    public async Task RemoveUnreferencedSchema_Keeps_Schemas_Matching_Pattern(
        SampleOpenSpecifications version,
        string pattern)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [pattern]);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Keys.Should().Contain(k => k.Contains("Pet"));
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, true)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, false)]
    public async Task RemoveUnreferencedSchema_Handles_IncludeInheritanceHierarchy_Flag(
        SampleOpenSpecifications version,
        bool includeHierarchy)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = includeHierarchy
        };
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Should().NotBeEmpty();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task SchemaCleaner_Constructor_Accepts_Parameters(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [".*"]);

        cleaner.Should().NotBeNull();
        cleaner.IncludeInheritanceHierarchy.Should().BeFalse();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    public async Task SchemaCleaner_IncludeInheritanceHierarchy_Can_Be_Set(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = true
        };

        cleaner.IncludeInheritanceHierarchy.Should().BeTrue();
    }

    [Test]
    public async Task RemoveUnreferencedSchema_Handles_Alias_Schemas_Sharing_The_Same_Instance()
    {
        const string spec = """
            openapi: 3.0.4
            info:
              title: Alias schema test
              version: "1"
            paths:
              /items:
                get:
                  operationId: GetItem
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/AliasItem'
            components:
              schemas:
                AliasItem:
                  $ref: '#/components/schemas/ActualItem'
                ActualItem:
                  type: object
                  properties:
                    id:
                      type: string
                UnusedItem:
                  type: object
                  properties:
                    ignored:
                      type: string
            """;

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);

        try
        {
            var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
            ReferenceEquals(document.Components.Schemas["AliasItem"], document.Components.Schemas["ActualItem"])
                .Should()
                .BeTrue();

            var cleaner = new SchemaCleaner(document, []);
            cleaner.Invoking(x => x.RemoveUnreferencedSchema()).Should().NotThrow();

            document.Components.Schemas.Should().ContainKey("AliasItem");
            document.Components.Schemas.Should().ContainKey("ActualItem");
            document.Components.Schemas.Should().NotContainKey("UnusedItem");
        }
        finally
        {
            if (File.Exists(swaggerFile))
            {
                File.Delete(swaggerFile);
            }

            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
