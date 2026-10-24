#:sdk Microsoft.NET.Sdk.Web@11
#:package FastEndpoints.OpenApi@9.0.0-beta.2
#:package Microsoft.AspNetCore.OpenApi@11.0.0-rc.2.26504.105
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder();
builder.Services.AddFastEndpoints();
builder.Services.OpenApiDocument();

var app = builder.Build();

app.MapFastEndpoints();
app.MapOpenApi();

await app.RunAsync();

public record MyStreamData(string Message, int Index);

public sealed class MyStreamEndpoint
    : EndpointWithoutRequest<ServerSentEventsResult<MyStreamData>>
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.ResultAsync(TypedResults.ServerSentEvents(Enumerable.Range(0, 10).ToAsyncEnumerable().Select(x => new MyStreamData("Hello World!", x))));
    }
}
