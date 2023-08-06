using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class MultipleInterfaceByTagGeneratorTests
{
    [Theory]
    [InlineData(typeof(ByTag.IPetApi))]
    [InlineData(typeof(ByTag.IUserApi))]
    [InlineData(typeof(ByTag.IStoreApi))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.ByTag");
}