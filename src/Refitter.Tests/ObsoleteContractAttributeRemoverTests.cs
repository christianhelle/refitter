using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class ObsoleteContractAttributeRemoverTests
{
    private const string Contracts = """
        namespace TestNamespace
        {
            /// <summary>
            /// Used by the interface
            /// </summary>
            [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.7.1.0")]
            [System.Obsolete("use E")]
            public partial class Used
            {
                [System.Obsolete]
                public string Old { get; set; }
            }

            [System.Obsolete]
            [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.7.1.0")]
            public partial record UsedRecord
            {
            }

            [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.7.1.0")]
            [System.Obsolete]
            public partial class Unused
            {
            }
        }
        """;

    private const string Interface = """
        public partial interface IApi
        {
            [Get("/used")]
            Task<ICollection<Used>> GetUsed([Body] UsedRecord body);
        }
        """;

    [Test]
    public void Removes_Obsolete_Attribute_From_Types_Referenced_By_Interfaces()
    {
        var result = ObsoleteContractAttributeRemover.Remove(Contracts, [Interface]);

        result.Should().NotContain("[System.Obsolete(\"use E\")]");
        result.Should().Contain(
            """
                [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.7.1.0")]
                public partial class Used
            """);
        result.Should().Contain(
            """
                /// <summary>
                /// Used by the interface
                /// </summary>
            """);
        result.Should().Contain(
            """
                public partial class Used
                {
            """);
        result.Should().Contain(
            """

                [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.7.1.0")]
                public partial record UsedRecord
            """);
    }

    [Test]
    public void Keeps_Obsolete_Attribute_On_Properties()
    {
        var result = ObsoleteContractAttributeRemover.Remove(Contracts, [Interface]);

        result.Should().Contain(
            """
                    [System.Obsolete]
                    public string Old { get; set; }
            """);
    }

    [Test]
    public void Keeps_Obsolete_Attribute_On_Types_Not_Referenced_By_Interfaces()
    {
        var result = ObsoleteContractAttributeRemover.Remove(Contracts, [Interface]);

        result.Should().Contain(
            """
                [System.Obsolete]
                public partial class Unused
            """);
    }

    [Test]
    [Arguments("internal partial class")]
    [Arguments("public enum")]
    [Arguments("internal enum")]
    [Arguments("public sealed partial class")]
    public void Removes_Obsolete_Attribute_From_Other_Declaration_Forms(string declaration)
    {
        var contracts = $$"""
            namespace TestNamespace
            {
                [System.Obsolete]
                {{declaration}} Used
                {
                }
            }
            """;

        var result = ObsoleteContractAttributeRemover.Remove(contracts, [Interface]);

        result.Should().NotContain("[System.Obsolete]");
        result.Should().Contain($"{declaration} Used");
    }

    [Test]
    public void Returns_Contracts_Unchanged_Without_Interfaces()
    {
        ObsoleteContractAttributeRemover.Remove(Contracts, []).Should().Be(Contracts);
    }

    [Test]
    public void Handles_Crlf_Line_Endings()
    {
        var contracts = Contracts.Replace("\r\n", "\n").Replace("\n", "\r\n");

        var result = ObsoleteContractAttributeRemover.Remove(contracts, [Interface]);

        result.Should().NotContain("[System.Obsolete(\"use E\")]");
        result.Should().Contain("[System.Obsolete]\r\n    public partial class Unused");
        result.Should().Contain("\"14.7.1.0\")]\r\n    public partial class Used\r\n");
    }
}
