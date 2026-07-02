using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class ReferenceResolutionExceptionTests
{
    [Test]
    public void Constructor_With_Message_Sets_Message()
    {
        var exception = new ReferenceResolutionException("reference resolution failed");

        exception.Message.Should().Be("reference resolution failed");
    }

    [Test]
    public void Constructor_With_Message_And_InnerException_Sets_InnerException()
    {
        var innerException = new InvalidOperationException("inner");

        var exception = new ReferenceResolutionException("outer", innerException);

        exception.Message.Should().Be("outer");
        exception.InnerException.Should().BeSameAs(innerException);
    }
}
