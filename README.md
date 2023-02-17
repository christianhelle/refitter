[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)
[![Smoke Tests](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml)

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

public interface ISwaggerPetstoreOpenAPI30
{
    [Put("/pet")]
    Task<Pet> UpdatePet();

    [Post("/pet")]
    Task<Pet> AddPet();

    [Get("/pet/findByStatus")]
    Task<System.Collections.Generic.ICollection<Pet>> FindPetsByStatus();

    [Get("/pet/findByTags")]
    Task<System.Collections.Generic.ICollection<Pet>> FindPetsByTags();

    [Get("/pet/{petId}")]
    Task<Pet> GetPetById(string petId);

    [Post("/pet/{petId}")]
    Task UpdatePetWithForm(string petId);

    [Delete("/pet/{petId}")]
    Task DeletePet(string petId);

    [Post("/pet/{petId}/uploadImage")]
    Task<ApiResponse> UploadFile(string petId);

    [Get("/store/inventory")]
    Task<System.Collections.Generic.IDictionary<string, int>> GetInventory();

    [Post("/store/order")]
    Task<Order> PlaceOrder();

    [Get("/store/order/{orderId}")]
    Task<Order> GetOrderById(string orderId);

    [Delete("/store/order/{orderId}")]
    Task DeleteOrder(string orderId);

    [Post("/user")]
    Task CreateUser();

    [Post("/user/createWithList")]
    Task<User> CreateUsersWithListInput();

    [Get("/user/login")]
    Task<string> LoginUser();

    [Get("/user/logout")]
    Task LogoutUser();

    [Get("/user/{username}")]
    Task<User> GetUserByName(string username);

    [Put("/user/{username}")]
    Task UpdateUser(string username);

    [Delete("/user/{username}")]
    Task DeleteUser(string username);
}
```

## System requirements
.NET 7.0

#

For tips and tricks on software development, check out [my blog](https://christianhelle.com)

If you find this useful and feel a bit generous then feel free to [buy me a coffee ☕](https://www.buymeacoffee.com/christianhelle)
