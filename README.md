[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)
[![Smoke Tests](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml)
[![NuGet](https://img.shields.io/nuget/v/refitter.svg?style=flat-square)](http://www.nuget.org/packages/refitter)

# Refitter
Refitter is a CLI tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. 

## Usage:

To generate code from an OpenAPI specifications file, run the following:

```
$ refitter [path to OpenAPI spec file]
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

The generated interface above can be used like this:

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

The code above when run will output something like this:

```
Name: Gatitotototo
Category: Chaucito
Status: Sold
```

## System requirements
.NET 7.0

#

For tips and tricks on software development, check out [my blog](https://christianhelle.com)

If you find this useful and feel a bit generous then feel free to [buy me a coffee ☕](https://www.buymeacoffee.com/christianhelle)
