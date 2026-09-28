using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class UniquePropertyNameGeneratorTests
{
    [Test]
    public void Suffixes_Properties_That_Normalize_To_The_Same_Name()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "user_name", "userName", "UserName", "email");

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null), schema);

        names.Should().Equal("UserName", "UserName2", "UserName3", "Email");
    }

    [Test]
    public void Avoids_The_Enclosing_Type_Name()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "order", "id");

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => "Order"), schema);

        names.Should().Equal("Order2", "Id");
    }

    [Test]
    public void Avoids_The_Generated_Additional_Properties_Member()
    {
        var schema = CreateSchema(allowAdditionalProperties: true, "AdditionalProperties");

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null), schema);

        names.Should().Equal("AdditionalProperties2");
    }

    [Test]
    public void Allows_Additional_Properties_Name_When_No_Dictionary_Is_Generated()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "AdditionalProperties");

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null), schema);

        names.Should().Equal("AdditionalProperties");
    }

    [Test]
    public void Reserves_Additional_Properties_For_Typed_Additional_Properties()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "AdditionalProperties");
        schema.AdditionalPropertiesSchema = new ApiSchema { Type = ApiObjectTypes.String };

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null), schema);

        names.Should().Equal("AdditionalProperties2");
    }

    [Test]
    public void Ignores_Inheritance_Discriminator_Properties_That_Are_Not_Emitted()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "$type", "type");
        schema.DiscriminatorObject = new ApiDiscriminator { PropertyName = "$type" };

        var names = GenerateAll(new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null), schema);

        names.Should().Equal("Type", "Type");
    }

    [Test]
    public void Delegates_To_Inner_Generator_For_Properties_Without_Parent()
    {
        var property = new ApiSchemaProperty();

        var name = new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null).Generate(property);

        name.Should().Be("_");
    }

    [Test]
    public void Returns_The_Same_Names_When_Generated_Again()
    {
        var schema = CreateSchema(allowAdditionalProperties: false, "a", "A");
        var generator = new UniqueContractPropertyNameGenerator(new ContractPropertyNameGenerator(), _ => null);

        GenerateAll(generator, schema).Should().Equal("A", "A2");
        GenerateAll(generator, schema).Should().Equal("A", "A2");
    }

    private static ApiSchema CreateSchema(bool allowAdditionalProperties, params string[] propertyNames)
    {
        var schema = new ApiSchema { Type = ApiObjectTypes.Object, AllowAdditionalProperties = allowAdditionalProperties };
        foreach (var propertyName in propertyNames)
        {
            schema.Properties[propertyName] = new ApiSchemaProperty { Type = ApiObjectTypes.String };
        }

        return schema;
    }

    private static List<string> GenerateAll(UniqueContractPropertyNameGenerator generator, ApiSchema schema) =>
        schema.Properties.Values.Select(generator.Generate).ToList();
}
