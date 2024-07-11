## RestService

Here's an example usage of the generated code above

```cs
using Refit;
using System;
using System.Threading.Tasks;

namespace Your.Namespace.Of.Choice.GeneratedCode;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = RestService.For<ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
        var pet = await client.GetPetById(1);

        Console.WriteLine("## Using Task<T> as return type ##");
        Console.WriteLine($"Name: {pet.Name}");
        Console.WriteLine($"Category: {pet.Category.Name}");
        Console.WriteLine($"Status: {pet.Status}");
        Console.WriteLine();

        var client2 = RestService.For<WithApiResponse.ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
        var response = await client2.GetPetById(2);

        Console.WriteLine("## Using Task<IApiResponse<T>> as return type ##");
        Console.WriteLine($"HTTP Status Code: {response.StatusCode}");
        Console.WriteLine($"Name: {response.Content.Name}");
        Console.WriteLine($"Category: {response.Content.Category.Name}");
        Console.WriteLine($"Status: {response.Content.Status}");
    }
}
```

The `RestService` class generates an implementation of `ISwaggerPetstore` that uses `HttpClient` to make its calls. 

The code above when run will output something like this:

```
## Using Task<T> as return type ##
Name: Gatitotototo
Category: Chaucito
Status: Sold

## Using Task<IApiResponse<T>> as return type ##
HTTP Status Code: OK
Name: Gatitotototo
Category: Chaucito
Status: Sold
```

## HttpClientFactory

Here's an example Minimal API with the [`Refit.HttpClientFactory`](https://www.nuget.org/packages/Refit.HttpClientFactory) library:

```cs
using Refit;
using Your.Namespace.Of.Choice.GeneratedCode;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddRefitClient<ISwaggerPetstore>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"));

var app = builder.Build();
app.MapGet(
        "/pet/{id:long}",
        async (ISwaggerPetstore petstore, long id) =>
        {
            try
            {
                return Results.Ok(await petstore.GetPetById(id));
            }
            catch (Refit.ApiException e)
            {
                return Results.StatusCode((int)e.StatusCode);
            }
        })
    .WithName("GetPetById")
    .WithOpenApi();

app.UseHttpsRedirection();
app.UseSwaggerUI();
app.UseSwagger();
app.Run();
```

.NET Core supports registering the generated `ISwaggerPetstore` interface via `HttpClientFactory`

The following request to the API above
```shell
$ curl -X 'GET' 'https://localhost:5001/pet/1' -H 'accept: application/json'
```

Returns a response that looks something like this:
```json
{
  "id": 1,
  "name": "Special_char_owner_!@#$^&()`.testing",
  "photoUrls": [
    "https://petstore3.swagger.io/resources/photos/623389095.jpg"
  ],
  "tags": [],
  "status": "Sold"
}
```

## Dependency Injection

Refitter supports generating bootstrapping code that allows the user to conveniently configure all generated Refit interfaces by calling a single extension method to `IServiceCollection`.

This is enabled through the `.refitter` settings file like this:

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "TelemetryDelegatingHandler" ],
    "transientErrorHandler": "Polly",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  }
}
```

which will generate an extension method to `IServiceCollection` called `ConfigureRefitClients()`. The generated extension method depends on [`Refit.HttpClientFactory`](https://www.nuget.org/packages/Refit.HttpClientFactory) library and looks like this:

```cs
public static IServiceCollection ConfigureRefitClients(
    this IServiceCollection services, 
    Action<IHttpClientBuilder>? builder = default, 
    RefitSettings? settings = default)
{
    var clientBuilderISwaggerPetstore = services
        .AddRefitClient<ISwaggerPetstore>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderISwaggerPetstore
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderISwaggerPetstore);

    return services;
}
```

This comes in handy especially when generating multiple interfaces, by tag or endpoint. For example, the following `.refitter` settings file

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "multipleInterfaces": "ByTag",
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "TelemetryDelegatingHandler" ],
    "transientErrorHandler": "Polly",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  }
}
```

Will generate a single `ConfigureRefitClients()` extension methods that may contain dependency injection configuration code for multiple interfaces like this

```csharp
public static IServiceCollection ConfigureRefitClients(
    this IServiceCollection services, 
    Action<IHttpClientBuilder>? builder = default, 
    RefitSettings? settings = default)
{
    var clientBuilderIPetApi = services
        .AddRefitClient<IPetApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIPetApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIPetApi);

    var clientBuilderIStoreApi = services
        .AddRefitClient<IStoreApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIStoreApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIStoreApi);

    var clientBuilderIUserApi = services
        .AddRefitClient<IUserApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIUserApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIUserApi);

    return services;
}
```

Personally, they I use Refitter is to generate an interface per endpoint, so when generating code for a large and complex API, I might have several interfaces.