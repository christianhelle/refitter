using AwesomeAssertions;
using NJsonSchema;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class DocumentEquivalenceComparerTraversalTests
{
    private static readonly DocumentEquivalenceComparer Comparer = new();

    [Test]
    public void AddReferencedSchemas_Traverses_AllOf_SubSchemas()
    {
        var definitions = new Dictionary<string, JsonSchema>();
        var root = new JsonSchema { Type = JsonObjectType.Object };
        root.AllOf.Add(Named("AllOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AllOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_OneOf_SubSchemas()
    {
        var definitions = new Dictionary<string, JsonSchema>();
        var root = new JsonSchema { Type = JsonObjectType.Object };
        root.OneOf.Add(Named("OneOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("OneOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_AnyOf_SubSchemas()
    {
        var definitions = new Dictionary<string, JsonSchema>();
        var root = new JsonSchema { Type = JsonObjectType.Object };
        root.AnyOf.Add(Named("AnyOfChild"));

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("AnyOfChild");
    }

    [Test]
    public void AddReferencedSchemas_Traverses_Nested_Definitions()
    {
        var definitions = new Dictionary<string, JsonSchema>();
        var root = new JsonSchema { Type = JsonObjectType.Object };
        root.Definitions["Nested"] = Named("NestedChild");

        Comparer.AddReferencedSchemas(definitions, root);

        definitions.Should().ContainKey("NestedChild");
    }

    private static JsonSchema Named(string definitionName)
    {
        var schema = new JsonSchema { Type = JsonObjectType.Object };
        schema.Reference = new JsonSchema { Type = JsonObjectType.Object };
        ((NJsonSchema.References.IJsonReferenceBase)schema).ReferencePath = $"#/definitions/{definitionName}";
        return schema;
    }
}
