[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)
[![Smoke Tests](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml)
[![NuGet](https://img.shields.io/nuget/v/refitter.svg?style=flat-square)](http://www.nuget.org/packages/refitter)

# Refitter
Refitter is a CLI tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. 

## Usage:

To generate code from an OpenAPI specifications file, run the following:

```shell
$ refitter [path to OpenAPI spec file] --namespace "[Your.Namespace.Of.Choice.GeneratedCode]"
```

This will generate a file called `Output.cs` which contains the Refit interface and contract classes generated using [NSwag](https://github.com/RicoSuter/NSwag)


Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io)

```cs
using Refit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GeneratedCode
{
    public interface ISwaggerPetstore
    {
        [Put("/pet")]
        Task<Pet> UpdatePet([Body]Pet body);

        [Post("/pet")]
        Task<Pet> AddPet([Body]Pet body);

        [Get("/pet/findByStatus")]
        Task<ICollection<Pet>> FindPetsByStatus();

        [Get("/pet/findByTags")]
        Task<ICollection<Pet>> FindPetsByTags();

        [Get("/pet/{petId}")]
        Task<Pet> GetPetById(long? petId);

        [Post("/pet/{petId}")]
        Task UpdatePetWithForm(long? petId);

        [Delete("/pet/{petId}")]
        Task DeletePet(long? petId);

        [Post("/pet/{petId}/uploadImage")]
        Task<ApiResponse> UploadFile(long? petId, [Body]FileParameter body);

        [Get("/store/inventory")]
        Task<IDictionary<string, int>> GetInventory();

        [Post("/store/order")]
        Task<Order> PlaceOrder([Body]Order body);

        [Get("/store/order/{orderId}")]
        Task<Order> GetOrderById(long? orderId);

        [Delete("/store/order/{orderId}")]
        Task DeleteOrder(long? orderId);

        [Post("/user")]
        Task CreateUser([Body]User body);

        [Post("/user/createWithList")]
        Task<User> CreateUsersWithListInput([Body]ICollection<User> body);

        [Get("/user/login")]
        Task<string> LoginUser();

        [Get("/user/logout")]
        Task LogoutUser();

        [Get("/user/{username}")]
        Task<User> GetUserByName(string username);

        [Put("/user/{username}")]
        Task UpdateUser(string username, [Body]User body);

        [Delete("/user/{username}")]
        Task DeleteUser(string username);
    }
}
```

# Using the generated code

Here's an example usage of the generated code above

```cs
using Refit;
using System;
using System.Threading.Tasks;

namespace GeneratedCode;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = RestService.For<ISwaggerPetstore>("https://petstore3.swagger.io/api/v3");
        var pet = await client.GetPetById(2);

        Console.WriteLine($"Name: {pet.Name}");
        Console.WriteLine($"Category: {pet.Category.Name}");
        Console.WriteLine($"Status: {pet.Status}");
    }
}
```

The `RestService` class generates an implementation of `ISwaggerPetstore` that uses `HttpClient` to make its calls. 

The code above when run will output something like this:

```
Name: Gatitotototo
Category: Chaucito
Status: Sold
```

Here's an example Minimal API with the [`Refit.HttpClientFactory`](https://www.nuget.org/packages/Refit.HttpClientFactory) library:

```cs
using Refit;
using GeneratedCode;

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

## System requirements
.NET 7.0

#

For tips and tricks on software development, check out [my blog](https://christianhelle.com)

If you find this useful and feel a bit generous then feel free to [buy me a coffee ☕](https://www.buymeacoffee.com/christianhelle)
