using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class DocumentEquivalenceComparerTraversalTests
{
    private static readonly DocumentEquivalenceComparer Comparer = new();

    [Test]
    public void AddReferencedSchemas_Traverses_AllOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectType.Object };
        root.AllOf.Add(Named("AllOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AllOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_OneOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectType.Object };
        root.OneOf.Add(Named("OneOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("OneOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_AnyOf_SubSchemas()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectType.Object };
        root.AnyOf.Add(Named("AnyOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AnyOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_Nested_Definitions()
    {
        var definitions = new Dictionary<string, ApiSchema>();
        var root = new ApiSchema { Type = ApiObjectType.Object };
        root.Definitions["Nested"] = Named("NestedChild");

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("NestedChild");
    }

    private static ApiSchema Named(string definitionName)
    {
        var schema = new ApiSchema { Type = ApiObjectType.Object };
        schema.Reference = new ApiSchema { Type = ApiObjectType.Object };
        schema.ReferencePath = $"#/definitions/{definitionName}";
        return schema;
    }
}
