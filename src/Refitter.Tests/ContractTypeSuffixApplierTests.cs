using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class ContractTypeSuffixApplierTests
{
    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Null()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, null!);

        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Empty()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, string.Empty);

        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Whitespace()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "   ");

        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Applies_Suffix_When_Valid()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");

        result.Should().Contain("public partial class PetDto");
        result.Should().Contain("public enum PetStatusDto");
        result.Should().NotContain($"public partial class Pet{Environment.NewLine}");
        result.Should().NotContain($"public enum PetStatus{Environment.NewLine}");
    }

    [Test]
    public void ContractTypeSuffixApplier_Applies_Suffix_To_Record_And_Struct_Declarations()
    {
        const string code = """
                            namespace TestNamespace
                            {
                                public record PetRecord;

                                public struct PetStruct
                                {
                                }

                                public partial class Container
                                {
                                    public PetRecord Record { get; set; }
                                    public PetStruct Struct { get; set; }
                                }
                            }
                            """;

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");

        result.Should().Contain("public record PetRecordDto;");
        result.Should().Contain("public struct PetStructDto");
        result.Should().Contain("public PetRecordDto Record");
        result.Should().Contain("public PetStructDto Struct");
    }
}
