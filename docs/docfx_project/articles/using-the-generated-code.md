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