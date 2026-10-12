using AwesomeAssertions;
using TUnit.Core;

namespace Refitter.Tests.TestUtilities;

public class RefitRequestCaptureTests
{
    private const string Code = """
        using System.Threading.Tasks;
        using Refit;

        namespace Sample;

        public interface IPets
        {
            [Get("/pets/{id}")]
            Task GetPet(int id, [Query] string? name);
        }
        """;

    [Test]
    public async Task Captures_Request_Sent_By_Refit()
    {
        var request = await RefitRequestCapture.SendAsync(Code, "IPets", "GetPet", 1, "rex");

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.PathAndQuery.Should().Be("/pets/1?name=rex");
    }

    [Test]
    public async Task Reports_Compilation_Errors()
    {
        var act = async () => await RefitRequestCapture.SendAsync("public interface IBroken { Task", "IBroken", "M");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
