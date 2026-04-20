using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;

namespace Refitter.Tests;

public class JsonSerializerContextGeneratorTests
{
    [Test]
    public void Generate_Returns_Empty_When_No_Types()
    {
        var settings = CreateSettings();

        JsonSerializerContextGenerator.Generate("// no contracts", settings)
            .Should()
            .BeEmpty();
    }

    [Test]
    public void Generate_Uses_Contracts_Namespace_And_Strips_Interface_Prefix()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Pet
                {
                }
            }
            """;

        var settings = CreateSettings(interfaceName: "IMyApi", @namespace: "My.Clients", contractsNamespace: "My.Contracts");
        var result = JsonSerializerContextGenerator.Generate(contracts, settings);

        result.Should().Contain("namespace My.Contracts");
        result.Should().Contain("internal partial class MyApiSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
        result.Should().NotContain("IMyApiSerializerContext");
    }

    [Test]
    public void Generate_Registers_Types_Once()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Pet
                {
                }

                internal record Owner(string Name);

                public enum Status
                {
                    Active
                }

                public partial class Pet
                {
                    public Status CurrentStatus { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Pet))]");
        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Owner))]");
        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Status))]");
        result.Split("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Pet))]").Length.Should().Be(2);
    }

    [Test]
    public void Generate_Registers_Nested_Types_With_Qualified_Name()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Outer
                {
                    public partial class Inner
                    {
                    }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Outer))]");
        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Outer.Inner))]");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    [Test]
    public void Generate_Registers_Closed_Generic_Usages_And_Skips_Open_Generic_Declarations()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Pet
                {
                }

                public partial class Envelope<T>
                {
                    public T Payload { get; set; }
                }

                public partial class PetResponse
                {
                    public Envelope<Pet> Value { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Envelope<Pet>))]");
        result.Should().NotContain("typeof(Envelope))");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    [Test]
    public void Generate_Global_Qualifies_Types_Outside_The_Context_Namespace()
    {
        const string contracts = """
            namespace Shared.Contracts
            {
                public partial class SharedModel
                {
                }
            }

            namespace My.Contracts
            {
                public partial class LocalModel
                {
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(LocalModel))]");
        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(global::Shared.Contracts.SharedModel))]");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    private static RefitGeneratorSettings CreateSettings(
        string interfaceName = "IMyApi",
        string @namespace = "My.Clients",
        string contractsNamespace = "My.Contracts") =>
        new()
        {
            Namespace = @namespace,
            ContractsNamespace = contractsNamespace,
            Naming = new NamingSettings
            {
                InterfaceName = interfaceName
            }
        };
}
