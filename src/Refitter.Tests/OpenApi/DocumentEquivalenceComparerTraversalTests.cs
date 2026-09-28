using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class DocumentEquivalenceComparerTraversalTests
{
    [Test]
    public void AddReferencedSchemas_Traverses_AllOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectTypes.Object };
        root.AllOf.Add(Named("AllOfChild"));

        DocumentEquivalenceComparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AllOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_OneOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectTypes.Object };
        root.OneOf.Add(Named("OneOfChild"));

        DocumentEquivalenceComparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("OneOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_AnyOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectTypes.Object };
        root.AnyOf.Add(Named("AnyOfChild"));

        DocumentEquivalenceComparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AnyOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_Nested_Definitions()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectTypes.Object };
        root.Definitions["Nested"] = Named("NestedChild");

        DocumentEquivalenceComparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("NestedChild");
    }

    private static ApiSchema Named(string definitionName)
    {
        var schema = new ApiSchema { Type = ApiObjectTypes.Object };
        schema.Reference = new ApiSchema { Type = ApiObjectTypes.Object };
        schema.ReferencePath = $"#/definitions/{definitionName}";
        return schema;
    }
}
