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
    [Arguments(RefitClient.Reflection)]
    [Arguments(RefitClient.Generated)]
    public async Task Captures_Request_Sent_By_Refit(RefitClient client)
    {
        var request = await RefitRequestCapture.SendAsync(client, Code, "IPets", "GetPet", 1, "rex");

        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.PathAndQuery.Should().Be("/pets/1?name=rex");
    }

    [Test]
    [Arguments(RefitClient.Reflection)]
    [Arguments(RefitClient.Generated)]
    public async Task Reports_Compilation_Errors(RefitClient client)
    {
        var act = async () => await RefitRequestCapture.SendAsync(client, "public interface IBroken { Task", "IBroken", "M");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
