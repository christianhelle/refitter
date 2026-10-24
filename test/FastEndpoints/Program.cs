#:sdk Microsoft.NET.Sdk.Web@11
#:package FastEndpoints.OpenApi@9.0.0-beta.2
#:package Microsoft.AspNetCore.OpenApi@11.0.0-rc.2.26504.105
#:property PublishAot=false

using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder();
builder.Services.AddFastEndpoints();
builder.Services.OpenApiDocument(o =>
{
    o.ConfigureOpenApi = api =>
    {
        api.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;

        api.AddOperationTransformer(
            (operation, context, ct) =>
            {
                if (operation.Responses is null)
                    return Task.CompletedTask;

                foreach (var response in operation.Responses.Values)
                {
                    if (
                        response.Content is { } contents
                        && contents.TryGetValue("text/event-stream", out var content)
                        && content is OpenApiMediaType { ItemSchema: { } itemSchema } mediaType
                    )
                    {
                        mediaType.Schema ??= itemSchema;
                        mediaType.ItemSchema = null;
                    }
                }

                return Task.CompletedTask;
            }
        );
    };
});

var app = builder.Build();

app.MapFastEndpoints();
app.MapOpenApi();

await app.RunAsync();

namespace FastEndpointsSample
{
    public record MyStreamData(string Message, int Index);

    public sealed class MyStreamEndpoint : EndpointWithoutRequest<ServerSentEventsResult<MyStreamData>>
    {
        public override void Configure()
        {
            Get("/");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            await Send.ResultAsync(
                TypedResults.ServerSentEvents(
                    Enumerable.Range(0, 10).ToAsyncEnumerable().Select(x => new MyStreamData("Hello World!", x))
                )
            );
        }
    }
}
