[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)

# Refitter
Refitter is a CLI tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. 

## System requirements
.NET 7.0

## Usage:

To generate code from an OpenAPI specifications file, run the following:

```
$ refitter [path to OpenAPI spec file]
```

This will generate a file called `Output.cs` which contains the Refit interface and contract classes generated using [NSwag](https://github.com/RicoSuter/NSwag)


Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io)

```cs
public interface ISwaggerPetstoreOpenAPI30
{
    [Refit.Put("/pet")]
    System.Threading.Tasks.Task<Pet> UpdatePet();

    [Refit.Post("/pet")]
    System.Threading.Tasks.Task<Pet> AddPet();

    [Refit.Get("/pet/findByStatus")]
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<Pet>> FindPetsByStatus();

    [Refit.Get("/pet/findByTags")]
    System.Threading.Tasks.Task<System.Collections.Generic.ICollection<Pet>> FindPetsByTags();

    [Refit.Get("/pet/{petId}")]
    System.Threading.Tasks.Task<Pet> GetPetById(string petId);

    [Refit.Post("/pet/{petId}")]
    System.Threading.Tasks.Task UpdatePetWithForm(string petId);

    [Refit.Delete("/pet/{petId}")]
    System.Threading.Tasks.Task DeletePet(string petId);

    [Refit.Post("/pet/{petId}/uploadImage")]
    System.Threading.Tasks.Task<ApiResponse> UploadFile(string petId);

    [Refit.Get("/store/inventory")]
    System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<string, int>> GetInventory();

    [Refit.Post("/store/order")]
    System.Threading.Tasks.Task<Order> PlaceOrder();

    [Refit.Get("/store/order/{orderId}")]
    System.Threading.Tasks.Task<Order> GetOrderById(string orderId);

    [Refit.Delete("/store/order/{orderId}")]
    System.Threading.Tasks.Task DeleteOrder(string orderId);

    [Refit.Post("/user")]
    System.Threading.Tasks.Task CreateUser();

    [Refit.Post("/user/createWithList")]
    System.Threading.Tasks.Task<User> CreateUsersWithListInput();

    [Refit.Get("/user/login")]
    System.Threading.Tasks.Task<string> LoginUser();

    [Refit.Get("/user/logout")]
    System.Threading.Tasks.Task LogoutUser();

    [Refit.Get("/user/{username}")]
    System.Threading.Tasks.Task<User> GetUserByName(string username);

    [Refit.Put("/user/{username}")]
    System.Threading.Tasks.Task UpdateUser(string username);

    [Refit.Delete("/user/{username}")]
    System.Threading.Tasks.Task DeleteUser(string username);
}
```