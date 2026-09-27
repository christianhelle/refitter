using System.Collections;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class OpenApiModelTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    private static ApiOperation Operation(string operation, string schemaType = "\"openapi\": \"3.0.1\"") =>
        Load($$"""{ {{schemaType}}, "info": { "title": "T", "version": "1" }, "paths": { "/a": { "post": {{operation}} } } }""")
            .Paths["/a"]["post"];

    [Test]
    public void Property_Dictionary_Implements_The_Dictionary_Contract()
    {
        var owner = new ApiSchema();
        var properties = owner.Properties;
        var first = new ApiSchemaProperty();
        properties.Add("a", first);
        properties.Add(new KeyValuePair<string, ApiSchemaProperty>("b", new ApiSchemaProperty()));

        first.Name.Should().Be("a");
        first.Parent.Should().BeSameAs(owner);
        properties.IsReadOnly.Should().BeFalse();
        properties.Contains(new KeyValuePair<string, ApiSchemaProperty>("a", first)).Should().BeTrue();
        var array = new KeyValuePair<string, ApiSchemaProperty>[2];
        properties.CopyTo(array, 0);
        array[0].Key.Should().Be("a");
        ((IEnumerable)properties).Cast<object>().Should().HaveCount(2);
        properties.Remove(new KeyValuePair<string, ApiSchemaProperty>("a", first)).Should().BeTrue();
        properties.Keys.Should().Equal("b");
    }

    [Test]
    public void Schema_Dictionary_Implements_The_Dictionary_Contract()
    {
        var document = new ApiDocument();
        var schemas = document.Definitions;
        var schema = new ApiSchema();
        schemas.Add("a", schema);
        schemas.Add(new KeyValuePair<string, ApiSchema>("b", new ApiSchema()));

        schema.Parent.Should().BeSameAs(document.Components);
        schemas.IsReadOnly.Should().BeFalse();
        schemas.Contains(new KeyValuePair<string, ApiSchema>("a", schema)).Should().BeTrue();
        var array = new KeyValuePair<string, ApiSchema>[2];
        schemas.CopyTo(array, 0);
        array[1].Key.Should().Be("b");
        ((IEnumerable)schemas).Cast<object>().Should().HaveCount(2);
        schemas.Remove(new KeyValuePair<string, ApiSchema>("a", schema)).Should().BeTrue();
        schemas["b"] = null!;
        schemas.Should().BeEmpty();
    }

    [Test]
    public void Schema_List_Implements_The_List_Contract()
    {
        var owner = new ApiSchema();
        var list = owner.AllOf;
        var first = new ApiSchema();
        var second = new ApiSchema();
        list.Add(first);
        list.Insert(0, second);
        list[1] = first;

        second.Parent.Should().BeSameAs(owner);
        list.IsReadOnly.Should().BeFalse();
        list.Contains(first).Should().BeTrue();
        list.IndexOf(first).Should().Be(1);
        var array = new ApiSchema[2];
        list.CopyTo(array, 0);
        array[0].Should().BeSameAs(second);
        ((IEnumerable)list).Cast<object>().Should().HaveCount(2);
        list.Remove(second).Should().BeTrue();
        list.RemoveAt(0);
        list.Should().BeEmpty();
    }

    [Test]
    public void Discriminator_Name_Creates_A_Discriminator_Object()
    {
        var schema = new ApiSchema { Discriminator = "kind" };
        schema.DiscriminatorObject!.PropertyName.Should().Be("kind");

        schema.Discriminator = null;
        schema.DiscriminatorObject.Should().BeNull();
    }

    [Test]
    public void Inherited_Schema_Prefers_References_Then_Objects_Then_The_First_Schema()
    {
        var withObject = new ApiSchema();
        var objectSchema = new ApiSchema { Type = ApiObjectType.Object };
        withObject.AllOf.Add(new ApiSchema { Type = ApiObjectType.String });
        withObject.AllOf.Add(objectSchema);
        withObject.InheritedSchema.Should().BeSameAs(objectSchema);

        var withoutObject = new ApiSchema();
        var first = new ApiSchema { Type = ApiObjectType.String };
        withoutObject.AllOf.Add(first);
        withoutObject.AllOf.Add(new ApiSchema { Type = ApiObjectType.Integer });
        withoutObject.InheritedSchema.Should().BeSameAs(first);
    }

    [Test]
    public void Discriminators_Are_Found_On_Types_And_Base_Types()
    {
        var baseSchema = new ApiSchema { Type = ApiObjectType.Object, Discriminator = "kind" };
        var derived = new ApiSchema { Type = ApiObjectType.Object };
        derived.AllOf.Add(new ApiSchema { Reference = baseSchema });
        derived.AllOf.Add(new ApiSchema { Type = ApiObjectType.Object });
        derived.ResponsibleDiscriminatorObject.Should().BeSameAs(baseSchema.DiscriminatorObject);

        var alias = new ApiSchema { Reference = baseSchema };
        alias.ActualDiscriminatorObject.Should().BeSameAs(baseSchema.DiscriminatorObject);
    }

    [Test]
    public void Actual_Properties_Must_Be_Unique()
    {
        var schema = new ApiSchema { Type = ApiObjectType.Object };
        schema.Properties["name"] = new ApiSchemaProperty();
        var baseSchema = new ApiSchema { Type = ApiObjectType.Object };
        var mixin = new ApiSchema { Type = ApiObjectType.Object };
        mixin.Properties["name"] = new ApiSchemaProperty();
        schema.AllOf.Add(new ApiSchema { Reference = baseSchema });
        schema.AllOf.Add(mixin);

        var act = () => schema.ActualProperties;

        act.Should().Throw<InvalidOperationException>().WithMessage("*'name'*defined multiple times*");
    }

    [Test]
    public void Nullability_Considers_Enums_And_Extension_Data()
    {
        var enumeration = new ApiSchema { Type = ApiObjectType.String };
        enumeration.Enumeration.Add("a");
        enumeration.Enumeration.Add(null);
        enumeration.IsNullable(ApiSchemaType.OpenApi3).Should().BeTrue();

        var extension = new ApiSchema { Type = ApiObjectType.String, ExtensionData = new() { ["nullable"] = "true" } };
        extension.IsNullable(ApiSchemaType.OpenApi3).Should().BeTrue();

        var invalid = new ApiSchema { Type = ApiObjectType.String, ExtensionData = new() { ["nullable"] = "maybe" } };
        invalid.IsNullable(ApiSchemaType.OpenApi3).Should().BeFalse();
    }

    [Test]
    public void Actual_Schema_Detects_Cycles_And_Unresolved_References()
    {
        var a = new ApiSchema();
        var b = new ApiSchema { Reference = a };
        a.Reference = b;
        FluentActions.Invoking(() => a.ActualSchema).Should().Throw<InvalidOperationException>().WithMessage("*Cyclic*");

        var unresolved = new ApiSchema { ReferencePath = "#/definitions/Missing" };
        FluentActions.Invoking(() => unresolved.ActualSchema).Should().Throw<InvalidOperationException>().WithMessage("*not been resolved*");
    }

    [Test]
    public void Actual_Schema_Follows_A_Single_Referenced_AnyOf()
    {
        var target = new ApiSchema { Type = ApiObjectType.Object };
        var schema = new ApiSchema();
        schema.AnyOf.Add(new ApiSchema { Reference = target });

        schema.ActualSchema.Should().BeSameAs(target);
    }

    [Test]
    public void Required_Properties_Are_Kept_On_The_Parent_Schema()
    {
        var property = new ApiSchemaProperty { IsRequired = true };
        var schema = new ApiSchema();
        schema.Properties["name"] = property;
        schema.RequiredProperties.Should().Equal("name");

        property.IsRequired = true;
        schema.RequiredProperties.Should().Equal("name");

        property.IsRequired = false;
        schema.RequiredProperties.Should().BeEmpty();
    }

    [Test]
    public void Parameters_Referencing_Parameters_Use_Their_Schema()
    {
        var target = new ApiParameter { Schema = new ApiSchema { Type = ApiObjectType.Integer } };
        var parameter = new ApiParameter { Reference = target };

        parameter.ActualSchema.Type.Should().Be(ApiObjectType.Integer);
    }

    [Test]
    public void Parameter_Nullability_Depends_On_The_Specification()
    {
        new ApiParameter { IsRequired = false }.IsNullable(ApiSchemaType.Swagger2).Should().BeTrue();
        new ApiParameter { IsNullableRaw = false }.IsNullable(ApiSchemaType.Swagger2).Should().BeFalse();
        new ApiParameter { CustomSchema = new ApiSchema { IsNullableRaw = true } }.IsNullable(ApiSchemaType.OpenApi3).Should().BeTrue();
        new ApiParameter().IsNullable(ApiSchemaType.JsonSchema).Should().BeFalse();
    }

    [Test]
    public void Changing_A_Body_Parameter_Updates_The_Request_Body()
    {
        var operation = Operation("""
            {
              "requestBody": { "x-name": "payload", "content": { "application/json": { "schema": { "type": "string" } } } },
              "responses": { "200": { "description": "ok" } }
            }
            """);

        var body = operation.Parameters.Single(p => p.Kind == ApiParameterKind.Body);
        body.Name = "renamed";
        body.IsRequired = true;
        operation.RequestBody!.Name.Should().Be("renamed");
        operation.RequestBody.IsRequired.Should().BeTrue();

        operation.RequestBody.Description = "D";
        operation.RequestBody.Position = 1;
        operation.RequestBody.Content["text/plain"] = new ApiMediaType { Schema = new ApiSchema { Type = ApiObjectType.String } };
        operation.RequestBody.Content["text/plain"].Example = "e";
        operation.Parameters.Single(p => p.Kind == ApiParameterKind.Body).Description.Should().Be("D");
        ((IEnumerable)operation.RequestBody.Content).Cast<object>().Should().HaveCount(2);
        operation.RequestBody.Content.Clear();
        operation.RequestBody.Content.ContainsKey("text/plain").Should().BeFalse();

        new ApiRequestBody().ActualName.Should().Be("body");
    }

    [Test]
    [Arguments("""["application/xml"]""", true, false)]
    [Arguments("""["application/xml", "application/json"]""", false, false)]
    [Arguments("""["application/octet-stream"]""", false, true)]
    [Arguments("""["application/octet-stream", "*/*"]""", false, false)]
    [Arguments("""["multipart/form-data", "text/plain"]""", false, true)]
    public void Swagger2_Body_Parameters_Are_Classified_By_Consumes(string consumes, bool isXml, bool isBinary)
    {
        var operation = Operation($$"""
            {
              "consumes": {{consumes}},
              "parameters": [{ "name": "body", "in": "body", "schema": { "type": "string", "format": "binary" } }],
              "responses": { "200": { "description": "ok" } }
            }
            """, "\"swagger\": \"2.0\"");

        var parameter = operation.Parameters[0];
        parameter.IsXmlBodyParameter.Should().Be(isXml);
        parameter.IsBinaryBodyParameter.Should().Be(isBinary);
        parameter.HasBinaryBodyWithMultipleMimeTypes.Should().Be(isBinary && consumes.Contains(','));
    }

    [Test]
    [Arguments("""{ "application/xml": { "schema": { "type": "object" } } }""", true, false, false)]
    [Arguments("""{ "application/xml": {}, "application/json": {} }""", false, false, false)]
    [Arguments("""{ "application/octet-stream": { "schema": { "type": "string", "format": "binary" } } }""", false, true, false)]
    [Arguments("""{ "application/octet-stream": { "schema": { "type": "string", "format": "binary" } }, "image/*": { "schema": { "type": "string", "format": "binary" } } }""", false, true, true)]
    [Arguments("""{ "*/*": { "schema": { "type": "string" } } }""", false, false, false)]
    [Arguments("""{ "multipart/form-data": { "schema": { "type": "object" } }, "application/json": { "schema": { "type": "object" } } }""", false, false, false)]
    public void OpenApi3_Body_Parameters_Are_Classified_By_Content(string content, bool isXml, bool isBinary, bool multipleMimeTypes)
    {
        var operation = Operation($$"""
            { "requestBody": { "content": {{content}} }, "responses": { "200": { "description": "ok" } } }
            """);

        var parameter = operation.Parameters.Single(p => p.Kind == ApiParameterKind.Body);
        parameter.IsXmlBodyParameter.Should().Be(isXml);
        parameter.IsBinaryBodyParameter.Should().Be(isBinary);
        parameter.HasBinaryBodyWithMultipleMimeTypes.Should().Be(multipleMimeTypes);
    }

    [Test]
    public void Parameters_Outside_Operations_Are_Not_Binary_Bodies()
    {
        var parameter = new ApiParameter { Kind = ApiParameterKind.Body };

        parameter.IsXmlBodyParameter.Should().BeFalse();
        parameter.IsBinaryBodyParameter.Should().BeFalse();
        parameter.HasBinaryBodyWithMultipleMimeTypes.Should().BeFalse();
        new ApiParameter { Kind = ApiParameterKind.Query }.HasBinaryBodyWithMultipleMimeTypes.Should().BeFalse();
    }

    [Test]
    [Arguments("""{ "type": "string", "format": "binary" }""", """["application/json"]""", true)]
    [Arguments("""{ "type": "object", "properties": { "a": { "type": "string" } } }""", """["application/octet-stream"]""", false)]
    [Arguments("""{}""", """["application/octet-stream"]""", true)]
    [Arguments("""{}""", """["application/json"]""", false)]
    public void Swagger2_Responses_Are_Binary_By_Schema_And_Produces(string schema, string produces, bool isBinary)
    {
        var operation = Operation($$"""
            { "produces": {{produces}}, "responses": { "200": { "description": "ok", "schema": {{schema}} }, "204": { "description": "none" } } }
            """, "\"swagger\": \"2.0\"");

        operation.Responses["200"].IsBinary(operation).Should().Be(isBinary);
        operation.Responses["204"].IsBinary(operation).Should().BeFalse();
        operation.Responses["204"].IsEmpty(operation).Should().BeTrue();
    }

    [Test]
    public void OpenApi3_Responses_Are_Binary_By_Content()
    {
        var operation = Operation("""
            {
              "responses": {
                "200": { "description": "ok", "content": { "application/pdf": {} } },
                "201": { "description": "ok", "content": { "application/json": {} } }
              }
            }
            """);

        operation.Responses["200"].IsBinary(operation).Should().BeTrue();
        operation.Responses["201"].IsBinary(operation).Should().BeFalse();
        operation.Responses["201"].IsEmpty(operation).Should().BeFalse();
    }
}
