#:sdk Microsoft.NET.Sdk.Web@11
#:package Microsoft.AspNetCore.OpenApi@11.0.0-rc.2.26504.105
#:property PublishAot=false

using MinimalApiSse;

var builder = WebApplication.CreateBuilder();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet(
        "/",
        () =>
            TypedResults.ServerSentEvents(
                Enumerable.Range(0, 10).ToAsyncEnumerable().Select(x => new MyStreamData("Hello World!", x))
            )
    )
    .WithName("MyStreamEndpoint");

app.MapOpenApi();

await app.RunAsync();

namespace MinimalApiSse
{
    public record MyStreamData(string Message, int Index);
}
