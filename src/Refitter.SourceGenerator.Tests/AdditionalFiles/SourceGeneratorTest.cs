using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class SourceGeneratorTest
{
    [Fact]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstore)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles");
}