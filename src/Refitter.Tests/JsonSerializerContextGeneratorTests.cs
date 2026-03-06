using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class JsonSerializerContextGeneratorTests
{
    [Test]
    public void Generate_Returns_Empty_When_No_Types()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                // No types here
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().BeEmpty();
    }

    [Test]
    public void Generate_Returns_Empty_For_Empty_Contract()
    {
        var contractCode = string.Empty;
        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };

        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().BeEmpty();
    }

    [Test]
    public void Generate_Returns_Context_With_JsonSerializable_Attributes_For_Class()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet
                {
                    public string Name { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet))]");
        result.Should().Contain("internal partial class IMyApiSerializerContext : JsonSerializerContext");
    }

    [Test]
    public void Generate_Returns_Context_With_JsonSerializable_Attributes_For_Record()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public record Owner(string Name, int Age);
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Owner))]");
        result.Should().Contain("internal partial class IMyApiSerializerContext : JsonSerializerContext");
    }

    [Test]
    public void Generate_Returns_Context_With_JsonSerializable_Attributes_For_Enum()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public enum Status
                {
                    Available,
                    Pending,
                    Sold
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Status))]");
        result.Should().Contain("internal partial class IMyApiSerializerContext : JsonSerializerContext");
    }

    [Test]
    public void Generate_Includes_All_Type_Names()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet
                {
                    public string Name { get; set; }
                }

                public record Owner(string Name);

                public enum Status
                {
                    Available,
                    Pending
                }

                internal class InternalModel
                {
                    public int Id { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet))]");
        result.Should().Contain("[JsonSerializable(typeof(Owner))]");
        result.Should().Contain("[JsonSerializable(typeof(Status))]");
        result.Should().Contain("[JsonSerializable(typeof(InternalModel))]");
    }

    [Test]
    public void Generate_Sorts_Type_Names_Alphabetically()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Zebra { }
                public class Apple { }
                public class Mango { }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        var appleIndex = result.IndexOf("[JsonSerializable(typeof(Apple))]");
        var mangoIndex = result.IndexOf("[JsonSerializable(typeof(Mango))]");
        var zebraIndex = result.IndexOf("[JsonSerializable(typeof(Zebra))]");

        appleIndex.Should().BeLessThan(mangoIndex);
        mangoIndex.Should().BeLessThan(zebraIndex);
    }

    [Test]
    public void Generate_Uses_Interface_Name_For_Context_Class_Name()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet { }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IPetStoreApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().Contain("internal partial class IPetStoreApiSerializerContext : JsonSerializerContext");
    }

    [Test]
    public void Generate_Handles_Partial_Classes()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public partial class Pet
                {
                    public string Name { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet))]");
    }

    [Test]
    public void Generate_Handles_Internal_Classes()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                internal class InternalPet
                {
                    public string Name { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(InternalPet))]");
    }

    [Test]
    public void Generate_Handles_Internal_Records()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                internal record InternalOwner(string Name);
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(InternalOwner))]");
    }

    [Test]
    public void Generate_Handles_Internal_Enums()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                internal enum InternalStatus
                {
                    Active,
                    Inactive
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(InternalStatus))]");
    }

    [Test]
    public void Generate_Ignores_Private_Types()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                private class PrivatePet
                {
                    public string Name { get; set; }
                }

                public class PublicPet
                {
                    public string Name { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(PublicPet))]");
        result.Should().NotContain("PrivatePet");
    }

    [Test]
    public void Generate_Handles_Types_With_Underscores()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet_Model
                {
                    public string Name { get; set; }
                }

                public class _InternalModel
                {
                    public int Id { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet_Model))]");
        result.Should().Contain("[JsonSerializable(typeof(_InternalModel))]");
    }

    [Test]
    public void Generate_Handles_Types_With_Numbers()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet2
                {
                    public string Name { get; set; }
                }

                public class Model123
                {
                    public int Id { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet2))]");
        result.Should().Contain("[JsonSerializable(typeof(Model123))]");
    }

    [Test]
    public void Generate_Does_Not_Duplicate_Type_Names()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet
                {
                    public string Name { get; set; }
                }
            }

            namespace AnotherNamespace
            {
                public class Pet
                {
                    public string Name { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        var petCount = System.Text.RegularExpressions.Regex.Matches(result, @"\[JsonSerializable\(typeof\(Pet\)\)\]").Count;
        petCount.Should().Be(1, "duplicate type names should only appear once");
    }

    [Test]
    public void Generate_Creates_Closing_Brace()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public class Pet { }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().Contain("{");
        result.Should().Contain("}");
    }

    [Test]
    public void Generate_Handles_Mixed_Indentation()
    {
        var contractCode = @"
namespace MyNamespace
{
public class Pet
{
    public string Name { get; set; }
}

    internal record Owner(string Name);

        public enum Status
        {
            Active
        }
}
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet))]");
        result.Should().Contain("[JsonSerializable(typeof(Owner))]");
        result.Should().Contain("[JsonSerializable(typeof(Status))]");
    }

    [Test]
    public void Generate_Handles_Record_With_Block_Body()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public record Pet
                {
                    public string Name { get; init; }
                    public int Age { get; init; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("[JsonSerializable(typeof(Pet))]");
    }

    [Test]
    public void Generate_Handles_Multiple_Partial_Classes()
    {
        var contractCode = @"
            namespace MyNamespace
            {
                public partial class Pet
                {
                    public string Name { get; set; }
                }

                public partial class Pet
                {
                    public int Age { get; set; }
                }
            }
        ";

        var settings = new RefitGeneratorSettings { Naming = { InterfaceName = "IMyApi" } };
        var result = JsonSerializerContextGenerator.Generate(contractCode, settings);

        result.Should().NotBeNullOrWhiteSpace();
        var petCount = System.Text.RegularExpressions.Regex.Matches(result, @"\[JsonSerializable\(typeof\(Pet\)\)\]").Count;
        petCount.Should().Be(1, "partial class declarations should only generate one JsonSerializable attribute");
    }
}
