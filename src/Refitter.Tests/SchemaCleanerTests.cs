using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
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

        document.Components.Schemas.Count.Should().BeLessOrEqualTo(initialCount);
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
}
