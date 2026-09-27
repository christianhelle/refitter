using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

/// <summary>
/// Covers the less common paths of the native OpenAPI model and contract generation.
/// </summary>
public class NativeModelBranchTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    private static ApiDocument LoadWithBody() => Load("""
        {
          "openapi": "3.0.1",
          "info": { "title": "T", "version": "1" },
          "paths": {
            "/a": {
              "post": {
                "parameters": [{ "name": "q", "in": "query", "schema": { "type": "string" } }],
                "requestBody": { "content": { "application/json": { "schema": { "type": "string" } } } },
                "responses": {}
              }
            }
          },
          "components": { "requestBodies": { "Body": { "content": { "text/plain": { "schema": { "type": "string" } } } } } }
        }
        """);

    [Test]
    public void Keeps_The_Body_Parameter_In_Sync_With_The_Request_Body()
    {
        var document = LoadWithBody();
        var operation = document.Paths["/a"]["post"];
        var component = document.Components.RequestBodies["Body"];

        var query = operation.Parameters.Single(p => p.Kind == ApiParameterKind.Query);
        query.Style = ApiParameterStyle.Form;
        query.Explode = true;
        query.Style.Should().Be(ApiParameterStyle.Form);
        query.Explode.Should().BeTrue();

        operation.RequestBody!.Reference = component;
        operation.Parameters.Single(p => p.Kind == ApiParameterKind.Body).Schema.Should().BeSameAs(component.Content["text/plain"].Schema);
        component.Reference = null;

        component.Content.Add("application/xml", new ApiMediaType());
        component.Content["application/xml"].Schema = new ApiSchema();
        component.Content["application/xml"].Example = "e";
        operation.RequestBody.Content.Add("text/csv", new ApiMediaType());
        new ApiMediaType { Example = "e" }.Example.Should().Be("e");
    }

    [Test]
    public void Parameters_Referencing_Schemas_Use_Their_Own_Schema()
    {
        var parameter = new ApiParameter
        {
            Reference = new ApiSchema { Type = ApiObjectType.Integer },
            Schema = new ApiSchema { Type = ApiObjectType.String },
        };

        parameter.ActualSchema.Type.Should().Be(ApiObjectType.String);
    }

    private static ContractGenerator CreateGenerator()
    {
        var document = Load("""{ "openapi": "3.0.1", "info": { "title": "T", "version": "1" }, "paths": {} }""");
        return new ContractGeneratorFactory(new RefitGeneratorSettings(), document).Create();
    }

    [Test]
    public void Resolves_Untyped_Integer_Enumerations_And_Date_Types_Without_Settings()
    {
        var generator = CreateGenerator();
        var enumeration = new ApiSchema();
        enumeration.Enumeration.Add(1);
        generator.Resolver.Resolve(enumeration, false, "Level").Should().Be("Level");

        generator.Settings.DateType = null!;
        generator.Settings.DateTimeType = null!;
        generator.Settings.TimeType = null!;
        generator.Settings.TimeSpanType = null!;
        generator.Resolver.Resolve(new ApiSchema { Type = ApiObjectType.String, Format = "date" }, true, null).Should().Be("?");
        generator.Resolver.Resolve(new ApiSchema { Type = ApiObjectType.String, Format = "date-time" }, true, null).Should().Be("?");
        generator.Resolver.Resolve(new ApiSchema { Type = ApiObjectType.String, Format = "time" }, true, null).Should().Be("?");
        generator.Resolver.Resolve(new ApiSchema { Type = ApiObjectType.String, Format = "time-span" }, true, null).Should().Be("?");
    }

    [Test]
    public void Resolves_Integer_Formats_And_Pattern_Dictionaries()
    {
        var resolver = CreateGenerator().Resolver;

        resolver.Resolve(new ApiSchema { Type = ApiObjectType.Integer, Format = "byte" }, false, null).Should().Be("byte");
        resolver.Resolve(new ApiSchema { Type = ApiObjectType.Integer, Format = "long" }, true, null).Should().Be("long?");
        resolver.Resolve(new ApiSchema { Type = ApiObjectType.Integer, Minimum = -5_000_000_000 }, true, null).Should().Be("long?");

        var pattern = new ApiSchema { Type = ApiObjectType.Object, AllowAdditionalProperties = false };
        pattern.PatternProperties["^a"] = new ApiSchemaProperty { Type = ApiObjectType.Integer };
        resolver.Resolve(pattern, false, null).Should().EndWith("<string, int>");

        var mixed = new ApiSchema { Type = ApiObjectType.Object, AllowAdditionalProperties = false };
        mixed.PatternProperties["^a"] = new ApiSchemaProperty { Type = ApiObjectType.Integer };
        mixed.PatternProperties["^b"] = new ApiSchemaProperty { Type = ApiObjectType.String };
        resolver.Resolve(mixed, false, null).Should().EndWith("<string, object>");
    }

    [Test]
    public void Registers_Referenced_Schemas_Under_Their_Definition_Names()
    {
        var resolver = CreateGenerator().Resolver;
        var target = new ApiSchema { Type = ApiObjectType.Object };
        target.Properties["a"] = new ApiSchemaProperty { Type = ApiObjectType.String };
        var dictionary = new ApiSchema { Type = ApiObjectType.Object, AdditionalPropertiesSchema = new ApiSchema { Type = ApiObjectType.String } };

        resolver.RegisterSchemaDefinitions(new Dictionary<string, ApiSchema>
        {
            ["Alias"] = new ApiSchema { Reference = target },
            ["Map"] = dictionary,
            ["Any"] = new ApiSchema { Type = ApiObjectType.Object },
        });

        resolver.Types.Select(t => t.Value).Should().Equal("Alias", "Map", "Any");
        resolver.TryGetTypeName(target).Should().Be("Alias");
    }

    [Test]
    public void Names_Types_From_Any_Sequence_Of_Reserved_Names()
    {
        var reserved = new[] { "Pet", "Other" }.Where(n => n.Length > 0);

        new SafeContractTypeNameGenerator(new HashSet<string>()).Generate(new ApiSchema(), "Pet", reserved).Should().Be("Pet2");
        new SchemaTypeNameGenerator().Generate(new ApiSchema(), "Pet", reserved).Should().Be("Pet2");
    }

    [Test]
    [Arguments("a$b", "A_b")]
    [Arguments("$ab", "_ab")]
    [Arguments("éa", "_Éa")]
    [Arguments("a.b.Name", "Name")]
    public void Removes_Illegal_Characters_From_Type_Names(string hint, string expected)
    {
        new SchemaTypeNameGenerator().Generate(new ApiSchema(), hint, Array.Empty<string>()).Should().Be(expected);
    }

    [Test]
    [Arguments(null, "Empty")]
    [Arguments("", "Empty")]
    [Arguments("=", "Eq")]
    [Arguments("!=", "Ne")]
    [Arguments(">", "Gt")]
    [Arguments("<", "Lt")]
    [Arguments(">=", "Ge")]
    [Arguments("<=", "Le")]
    [Arguments("~=", "Approx")]
    [Arguments("-1", "Minus1")]
    [Arguments("+1", "Plus1")]
    [Arguments("_-a", "__a")]
    [Arguments("a:b", "AB")]
    public void Names_Enum_Members(string? name, string expected)
    {
        new DefaultContractEnumNameGenerator().Generate(0, name, name, new ApiSchema()).Should().Be(expected);
    }

    [Test]
    public void Skips_Inheritance_Discriminators_When_Naming_Properties()
    {
        var baseSchema = new ApiSchema { Type = ApiObjectType.Object, Discriminator = "kind" };
        baseSchema.Properties["kind"] = new ApiSchemaProperty { Type = ApiObjectType.String };
        var generator = new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null);

        generator.Generate(baseSchema.Properties["kind"]).Should().Be("Kind");
        generator.Generate(new ApiSchemaProperty { Name = "free" }).Should().Be("Free");
    }

    [Test]
    public void Renames_Properties_Named_Like_Their_Class_With_Plain_Property_Names()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {},
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "pet": { "type": "string" }, "pet1": { "type": "string" } } },
                  "Owner": { "type": "object", "properties": { "name": { "type": "string" } } }
                }
              }
            }
            """);
        var generator = new ContractGeneratorFactory(new RefitGeneratorSettings(), document).Create();
        generator.Settings.PropertyNameGenerator = new ContractPropertyNameGenerator();

        var code = generator.GenerateFile(new MultipleClientsFromOperationIdApiOperationNameGenerator());

        code.Should().Contain("public string Pet2 { get; set; }");
        code.Should().Contain("public string Name { get; set; }");
    }

    [Test]
    public void Describes_Ranges_For_Exclusive_Bounds_And_Other_Formats()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {},
              "components": {
                "schemas": {
                  "Measure": {
                    "type": "object",
                    "properties": {
                      "a": { "type": "integer", "format": "int64", "minimum": 1, "maximum": 5, "exclusiveMinimum": true, "exclusiveMaximum": true },
                      "b": { "type": "number", "format": "float", "minimum": 1, "maximum": 5, "exclusiveMinimum": true, "exclusiveMaximum": true },
                      "c": { "type": "number", "format": "float", "minimum": 1, "maximum": 5, "multipleOf": 2, "exclusiveMinimum": true, "exclusiveMaximum": true },
                      "d": { "type": "string", "x-deprecatedMessage": "use e" }
                    }
                  }
                }
              }
            }
            """);
        var generator = new ContractGeneratorFactory(new RefitGeneratorSettings { CodeGeneratorSettings = new CodeGeneratorSettings { GenerateDataAnnotations = true } }, document).Create();
        var model = new ClassTemplateModel("Measure", generator.Settings, generator.Resolver, document.Definitions["Measure"], document);
        var properties = model.Properties.ToDictionary(p => p.Name);

        properties["a"].RangeMinimumValue.Should().Be("2L");
        properties["a"].RangeMaximumValue.Should().Be("4L");
        properties["b"].RangeMinimumValue.Should().Be("1F");
        properties["c"].RangeMinimumValue.Should().Be("3F");
        properties["c"].RangeMaximumValue.Should().Be("3F");
        properties["d"].DeprecatedMessage.Should().Be("use e");
        properties["d"].RegularExpressionValue.Should().BeNull();
        model.BaseClassName.Should().BeNull();
    }

    [Test]
    public void Resolves_Types_Of_Form_Properties()
    {
        var settings = new RefitGeneratorSettings { OptionalParameters = true };
        var int64 = new RefitGeneratorSettings { CodeGeneratorSettings = new CodeGeneratorSettings { IntegerType = IntegerType.Int64 } };

        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Integer }, settings).Should().Be("int");
        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Integer, Format = "int32" }, settings).Should().Be("int");
        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Integer }, int64).Should().Be("long");
        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Boolean, IsNullableRaw = true }, settings).Should().Be("bool?");
        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Array, Item = new ApiSchema { Type = ApiObjectType.Number } }, settings).Should().Be("double[]");
        ParameterTypeResolver.GetCSharpType(new ApiSchema { Type = ApiObjectType.Array }, settings).Should().Be("object[]");
    }

    [Test]
    public void Canonical_Json_Sorts_And_Normalizes_Values()
    {
        var comparer = new DocumentEquivalenceComparer();
        var schema = new ApiSchema { Type = ApiObjectType.Object, IsNullableRaw = true };
        schema.AllOf.Add(new ApiSchema { Type = ApiObjectType.String });
        schema.AllOf.Add(new ApiSchema { Type = ApiObjectType.Integer });

        comparer.CreateCanonicalSchemaJson(schema, new HashSet<ApiSchema>()).Should().Contain("\"nullable\":true");

        var pathItem = new ApiPathItem { Summary = "S" };
        var operation = new ApiOperation { Tags = ["b", "a"], ExtensionData = new() { ["x-big"] = 1e300d, ["x-one"] = 1.50m } };
        pathItem.Add("get", operation);

        var json = comparer.CreateCanonicalJson(pathItem);
        json.Should().Contain("[\"b\",\"a\"]");
        json.Should().Contain("1E+300").And.Contain("\"x-one\":1.5");
        comparer.AreEquivalent(pathItem, pathItem).Should().BeTrue();
    }

    [Test]
    public void Names_Duplicate_Get_Operations_Returning_Arrays_GetAll()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/pets": { "get": { "operationId": "pets_getPets", "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "array", "items": { "type": "string" } } } } } } } },
                "/pets/{id}": { "get": { "operationId": "pets_getPets", "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "string" } } } } } } },
                "/owners": { "get": { "operationId": "pets_getPets", "responses": { "201": { "description": "ok" } } } }
              }
            }
            """);
        var generator = new MultipleClientsFromOperationIdApiOperationNameGenerator();
        string Name(string path) => generator.GetOperationName(document, path, "get", document.Paths[path]["get"]);

        Name("/pets").Should().Be("GetAllPets");
        Name("/pets/{id}").Should().Be("getPets");
        Name("/owners").Should().Be("getPets");
    }

    [Test]
    public void Client_Models_Create_The_Configured_Json_Converters()
    {
        var document = Load("""{ "openapi": "3.0.1", "info": { "title": "T", "version": "1" }, "paths": {} }""");
        var settings = new ContractGeneratorSettings { JsonConverters = ["A", "B"] };

        new ClientTemplateModel("Client", [], document, settings).JsonConvertersArrayCode
            .Should().Be("new System.Text.Json.Serialization.JsonConverter[] { new A(), new B() }");
        new ClientTemplateModel("Client", [], document, new ContractGeneratorSettings()).JsonConvertersArrayCode
            .Should().BeEmpty();
    }
}
