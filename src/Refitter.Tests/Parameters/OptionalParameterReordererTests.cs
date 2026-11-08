using AwesomeAssertions;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;

namespace Refitter.Tests.Parameters;

[TestClass]
public class OptionalParameterReordererTests
{
    private static readonly RefitGeneratorSettings OptionalParameters = new() { OptionalParameters = true };

    [TestMethod]
    public void Moves_Nullable_Parameters_Last_And_Defaults_Them()
    {
        var result = OptionalParameterReorderer.Reorder(
            [new("string?", "name"), new("long", "id"), new("int?", "page"), new("string", "tag")],
            OptionalParameters,
            new List<CSharpParameterModel>());

        MethodParameter.Render(result)
            .Should()
            .Be("long id, string tag, string? name = default, int? page = default");
    }

    [TestMethod]
    public void Keeps_Order_When_Optional_Parameters_Are_Disabled()
    {
        var parameters = new List<MethodParameter> { new("string?", "name"), new("long", "id") };

        OptionalParameterReorderer.Reorder(parameters, new RefitGeneratorSettings(), new List<CSharpParameterModel>())
            .Should()
            .Equal(parameters);
    }

    [TestMethod]
    public void Does_Not_Treat_Nullable_Type_Arguments_As_Optional()
    {
        var result = OptionalParameterReorderer.Reorder(
            [new("IEnumerable<int?>", "ids"), new("long", "id")],
            OptionalParameters,
            new List<CSharpParameterModel>());

        MethodParameter.Render(result).Should().Be("IEnumerable<int?> ids, long id");
    }
}
