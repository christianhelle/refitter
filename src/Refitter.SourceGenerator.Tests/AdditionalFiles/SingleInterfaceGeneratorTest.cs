using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class SingleInterfaceGeneratorTest
{
    [Fact]
    public void Should_Type_Exist() =>
        typeof(SingeInterface.ISwaggerPetstore)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.SingeInterface");
}