using AwesomeAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.SingeInterface;
using Refitter.Tests.CustomGenerated;

namespace Refitter.SourceGenerators.Tests;

public interface IHandWrittenApi
{
    [Get("/ping")]
    Task<string> Ping();
}

[TestClass]
public class RefitImplementationGeneratorTests
{
    private const string GeneratedImplementationNamespace = "Refit.Implementation.";

    [TestMethod]
    public void Refitter_Interface_Uses_Generated_Refit_Implementation() =>
        RestService.For<ISwaggerPetstoreInterface>("https://localhost")
            .GetType()
            .FullName
            .Should()
            .StartWith(GeneratedImplementationNamespace);

    [TestMethod]
    public void Refitter_Interface_Without_Contracts_Uses_Generated_Refit_Implementation() =>
        RestService.For<IApiInCustomGeneratedFolder>("https://localhost")
            .GetType()
            .FullName
            .Should()
            .StartWith(GeneratedImplementationNamespace);

    [TestMethod]
    public void Hand_Written_Interface_Uses_Generated_Refit_Implementation() =>
        RestService.For<IHandWrittenApi>("https://localhost")
            .GetType()
            .FullName
            .Should()
            .StartWith(GeneratedImplementationNamespace);
}
