using AwesomeAssertions;
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

    [Test]
    public void ContractTypeSuffixApplier_Renames_Generic_Type_References()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Page<T>
    {
        public System.Collections.Generic.ICollection<T> Items { get; set; }
    }

    public partial class Pet
    {
        public int Id { get; set; }
    }

    public partial class PetPage
    {
        public Page<Pet> Data { get; set; }
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");

        result.Should().Contain("class PageDto<T>");
        result.Should().Contain("PageDto<PetDto> Data");
        result.Should().Contain("ICollection<T> Items");
    }

    [Test]
    public void ContractTypeSuffixApplier_Does_Not_Rename_Generic_Framework_Type_Sharing_Contract_Name()
    {
        const string code = @"
namespace TestNamespace
{
    public partial interface ITasks
    {
        Task<System.Collections.Generic.ICollection<Task>> GetTasks();
    }

    public partial class Task
    {
        public int Id { get; set; }
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");

        result.Should().Contain("class TaskDto");
        result.Should().Contain("Task<System.Collections.Generic.ICollection<TaskDto>> GetTasks()");
    }

    [Test]
    public void ContractTypeSuffixApplier_Does_Not_Rename_System_Qualified_Type_Sharing_Contract_Name()
    {
        const string code = @"
namespace TestNamespace
{
    public partial interface ITasks
    {
        System.Threading.Tasks.Task DeleteTasks();
        global::System.Threading.Tasks.Task ClearTasks();
        TestNamespace.Task GetTask();
        System.Object Describe(Task task);
    }

    public partial class Task
    {
        public int Id { get; set; }
    }
}";

        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");

        result.Should().Contain("System.Threading.Tasks.Task DeleteTasks()");
        result.Should().Contain("global::System.Threading.Tasks.Task ClearTasks()");
        result.Should().Contain("TestNamespace.TaskDto GetTask()");
        result.Should().Contain("System.Object Describe(TaskDto task)");
    }
}
