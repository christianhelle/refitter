using AwesomeAssertions;
using Refit;
using Refitter.Tests.CaseInsensitiveRouteParameters;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

/// <summary>
/// Refit binds route placeholders case-insensitively, so a query parameter whose name only differs
/// by case from a path parameter must not be bound to the route (#1338).
/// </summary>
public class CaseInsensitiveRouteParametersTests
{
    [Test]
    public async Task Sends_Path_And_Query_Parameters_Differing_By_Case()
    {
        var handler = new CapturingHandler();
        var client = RestService.For<IRepro>(new HttpClient(handler) { BaseAddress = new Uri("https://localhost") });

        await client.GetSomething(1, 2);

        handler.RequestUri!.PathAndQuery.Should().Be("/api/parent/1/child?ID=2");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
