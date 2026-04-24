using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;

namespace Refitter.Tests;

public class JsonSerializerContextGeneratorTests
{
    [Test]
    public void Generate_Returns_Empty_When_Contracts_Are_Whitespace()
    {
        JsonSerializerContextGenerator.Generate(" \r\n\t ", CreateSettings())
            .Should()
            .BeEmpty();
    }

    [Test]
    public void Generate_Returns_Empty_When_No_Types()
    {
        var settings = CreateSettings();

        JsonSerializerContextGenerator.Generate("// no contracts", settings)
            .Should()
            .BeEmpty();
    }

    [Test]
    public void Generate_Uses_OpenApi_Title_For_Context_Name_When_Enabled()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Pet
                {
                }
            }
            """;

        var settings = CreateSettings(interfaceName: "IIgnoredApi");
        settings.Naming.UseOpenApiTitle = true;

        var result = JsonSerializerContextGenerator.Generate(contracts, settings, "PetService");

        result.Should().Contain("internal partial class PetServiceSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
        result.Should().NotContain("IgnoredApiSerializerContext");
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
    public void Generate_Falls_Back_To_Default_Context_Name_When_Interface_Name_Is_Missing()
    {
        const string contracts = """
            namespace My.Contracts
            {
                public partial class Pet
                {
                }
            }
            """;

        var settings = CreateSettings(interfaceName: null!);
        var result = JsonSerializerContextGenerator.Generate(contracts, settings);

        result.Should().Contain("internal partial class ApiClientSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext");
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
    public void Generate_Skips_Generic_Usages_That_Still_Reference_Open_Type_Parameters()
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

                public partial class Wrapper<T>
                {
                    public Envelope<T> OpenValue { get; set; }
                }

                public partial class ClosedWrapper
                {
                    public Envelope<Pet> ClosedValue { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Envelope<Pet>))]");
        result.Should().NotContain("typeof(Envelope<T>))");
        result.Should().NotContain("typeof(Wrapper))");
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

    [Test]
    public void Generate_Formats_Nullable_Array_And_Qualified_Generic_Usages()
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

                public partial class Container<T>
                {
                    public T Value { get; set; }
                }

                public partial class Response
                {
                    public Container<My.Contracts.Envelope<My.Contracts.Pet>[]?> QualifiedValues { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Container<Envelope<Pet>[]?>))]");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    [Test]
    public void Generate_Formats_Alias_Qualified_Generic_Usages()
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

                public partial class Container<T>
                {
                    public T Value { get; set; }
                }

                public partial class Response
                {
                    public Container<global::My.Contracts.Envelope<global::My.Contracts.Pet>> AliasedValue { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Container<global::My.Contracts.Envelope<global::My.Contracts.Pet>>))]");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    [Test]
    public void Generate_Formats_Alias_Qualified_Declared_Types_Using_Namespace_Alias()
    {
        const string contracts = """
            using Contracts = My.Contracts;

            namespace My.Contracts
            {
                public partial class Pet
                {
                }

                public partial class Envelope<T>
                {
                    public T Payload { get; set; }
                }

                public partial class Response
                {
                    public Contracts::Pet AliasedPet { get; set; }

                    public Contracts::Envelope<Pet> AliasedEnvelope { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Pet))]");
        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Envelope<Pet>))]");
        BuildHelper.BuildCSharp(contracts, result).Should().BeTrue();
    }

    [Test]
    public void Generate_Formats_NonDeclared_Generic_Type_Arguments()
    {
        const string contracts = """
            using System.Collections.Generic;

            namespace My.Contracts
            {
                public partial class Pet
                {
                }

                public partial class Envelope<T>
                {
                    public T Payload { get; set; }
                }

                public partial class Response
                {
                    public Envelope<Dictionary<string, Pet>> Value { get; set; }
                }
            }
            """;

        var result = JsonSerializerContextGenerator.Generate(contracts, CreateSettings());

        result.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Envelope<Dictionary<string, Pet>>))]");
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
