using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ClassBaseRecordConverterTests
{
    private const string Contracts = """
        namespace TestNamespace
        {
            public partial class ScopeMap : System.Collections.Generic.Dictionary<string, Scope>
            {
            }

            public partial record DetailedScopeMap : ScopeMap
            {
                public string Owner { get; init; }
            }

            public partial record AuditedScopeMap : TestNamespace.DetailedScopeMap
            {
                public string Auditor { get; init; }
            }

            public partial record Scope
            {
                public string Name { get; init; }
            }

            public partial record NamedScope : Scope
            {
            }
        }
        """;

    [TestMethod]
    public void Converts_Record_Deriving_From_Class()
    {
        var result = ClassBaseRecordConverter.Convert(Contracts);

        result.Should().Contain("public partial class DetailedScopeMap : ScopeMap");
    }

    [TestMethod]
    public void Converts_Records_Further_Down_The_Inheritance_Chain()
    {
        var result = ClassBaseRecordConverter.Convert(Contracts);

        result.Should().Contain("public partial class AuditedScopeMap : TestNamespace.DetailedScopeMap");
    }

    [TestMethod]
    public void Keeps_Records_Deriving_From_Records()
    {
        var result = ClassBaseRecordConverter.Convert(Contracts);

        result.Should().Contain("public partial record Scope");
        result.Should().Contain("public partial record NamedScope : Scope");
    }

    [TestMethod]
    public void Leaves_Contracts_Without_Records_Unchanged()
    {
        const string contracts = """
            public partial class Base
            {
            }

            public partial class Derived : Base
            {
            }
            """;

        ClassBaseRecordConverter.Convert(contracts).Should().BeSameAs(contracts);
    }
}
