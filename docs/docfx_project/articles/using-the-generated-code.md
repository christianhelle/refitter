# Using the generated code

Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io) using the default settings

**CLI Tool**

```bash
$ refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode"
```

**Source Generator ***.refitter*** file**

```json
{
  "openApiPath": "./openapi.json",
  "namespace": "Your.Namespace.Of.Choice.GeneratedCode"
}
```

**Output**

```cs
using Refit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Your.Namespace.Of.Choice.GeneratedCode
{
    public partial interface ISwaggerPetstore
    {
        /// <summary>
        /// Update an existing pet by Id
        /// </summary>
        [Put("/pet")]
        Task<Pet> UpdatePet([Body] Pet body);

        /// <summary>
        /// Add a new pet to the store
        /// </summary>
        [Post("/pet")]
        Task<Pet> AddPet([Body] Pet body);

        /// <summary>
        /// Multiple status values can be provided with comma separated strings
        /// </summary>
        [Get("/pet/findByStatus")]
        Task<ICollection<Pet>> FindPetsByStatus([Query] Status? status);

        /// <summary>
        /// Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.
        /// </summary>
        [Get("/pet/findByTags")]
        Task<ICollection<Pet>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

        /// <summary>
        /// Returns a single pet
        /// </summary>
        [Get("/pet/{petId}")]
        Task<Pet> GetPetById(long petId);

        [Post("/pet/{petId}")]
        Task UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

        [Delete("/pet/{petId}")]
        Task DeletePet(long petId, [Header("api_key")] string api_key);

        [Post("/pet/{petId}/uploadImage")]
        Task<ApiResponse> UploadFile(long petId, [Query] string additionalMetadata, StreamPart body);

        /// <summary>
        /// Returns a map of status codes to quantities
        /// </summary>
        [Get("/store/inventory")]
        Task<IDictionary<string, int>> GetInventory();

        /// <summary>
        /// Place a new order in the store
        /// </summary>
        [Post("/store/order")]
        Task<Order> PlaceOrder([Body] Order body);

        /// <summary>
        /// For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions
        /// </summary>
        [Get("/store/order/{orderId}")]
        Task<Order> GetOrderById(long orderId);

        /// <summary>
        /// For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors
        /// </summary>
        [Delete("/store/order/{orderId}")]
        Task DeleteOrder(long orderId);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Post("/user")]
        Task CreateUser([Body] User body);

        /// <summary>
        /// Creates list of users with given input array
        /// </summary>
        [Post("/user/createWithList")]
        Task<User> CreateUsersWithListInput([Body] IEnumerable<User> body);

        [Get("/user/login")]
        Task<string> LoginUser([Query] string username, [Query] string password);

        [Get("/user/logout")]
        Task LogoutUser();

        [Get("/user/{username}")]
        Task<User> GetUserByName(string username);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Put("/user/{username}")]
        Task UpdateUser(string username, [Body] User body);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Delete("/user/{username}")]
        Task DeleteUser(string username);
    }
}
```

Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io) configured to wrap the return type in `IApiResponse<T>`

**CLI Tool**

```bash
$ refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode" --use-api-response
```

**Source Generator ***.refitter*** file**

```json
{
  "openApiPath": "./openapi.json",
  "namespace": "Your.Namespace.Of.Choice.GeneratedCode",
  "returnIApiResponse": true
}
```

**Output**

```cs
using Refit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Your.Namespace.Of.Choice.GeneratedCode.WithApiResponse
{
    public partial interface ISwaggerPetstore
    {
        /// <summary>
        /// Update an existing pet by Id
        /// </summary>
        [Put("/pet")]
        Task<IApiResponse<Pet>> UpdatePet([Body] Pet body);

        /// <summary>
        /// Add a new pet to the store
        /// </summary>
        [Post("/pet")]
        Task<IApiResponse<Pet>> AddPet([Body] Pet body);

        /// <summary>
        /// Multiple status values can be provided with comma separated strings
        /// </summary>
        [Get("/pet/findByStatus")]
        Task<IApiResponse<ICollection<Pet>>> FindPetsByStatus([Query] Status? status);

        /// <summary>
        /// Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.
        /// </summary>
        [Get("/pet/findByTags")]
        Task<IApiResponse<ICollection<Pet>>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

        /// <summary>
        /// Returns a single pet
        /// </summary>
        [Get("/pet/{petId}")]
        Task<IApiResponse<Pet>> GetPetById(long petId);

        [Post("/pet/{petId}")]
        Task UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

        [Delete("/pet/{petId}")]
        Task DeletePet(long petId, [Header("api_key")] string api_key);

        [Post("/pet/{petId}/uploadImage")]
        Task<IApiResponse<ApiResponse>> UploadFile(long petId, [Query] string additionalMetadata, StreamPart body);

        /// <summary>
        /// Returns a map of status codes to quantities
        /// </summary>
        [Get("/store/inventory")]
        Task<IApiResponse<IDictionary<string, int>>> GetInventory();

        /// <summary>
        /// Place a new order in the store
        /// </summary>
        [Post("/store/order")]
        Task<IApiResponse<Order>> PlaceOrder([Body] Order body);

        /// <summary>
        /// For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions
        /// </summary>
        [Get("/store/order/{orderId}")]
        Task<IApiResponse<Order>> GetOrderById(long orderId);

        /// <summary>
        /// For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors
        /// </summary>
        [Delete("/store/order/{orderId}")]
        Task DeleteOrder(long orderId);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Post("/user")]
        Task CreateUser([Body] User body);

        /// <summary>
        /// Creates list of users with given input array
        /// </summary>
        [Post("/user/createWithList")]
        Task<IApiResponse<User>> CreateUsersWithListInput([Body] IEnumerable<User> body);

        [Get("/user/login")]
        Task<IApiResponse<string>> LoginUser([Query] string username, [Query] string password);

        [Get("/user/logout")]
        Task LogoutUser();

        [Get("/user/{username}")]
        Task<IApiResponse<User>> GetUserByName(string username);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Put("/user/{username}")]
        Task UpdateUser(string username, [Body] User body);

        /// <summary>
        /// This can only be done by the logged in user.
        /// </summary>
        [Delete("/user/{username}")]
        Task DeleteUser(string username);
    }
}
```

Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io) configured to generate an interface for each endpoint

**CLI Tool**

```bash
$ refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode" --multiple-interfaces ByEndpoint
```

**Source Generator ***.refitter*** file**

```json
{
  "openApiPath": "./openapi.json",
  "namespace": "Your.Namespace.Of.Choice.GeneratedCode",
  "multipleInterfaces": "ByEndpoint"
}
```

**Output**

```cs
/// <summary>
/// Update an existing pet
/// </summary>
public partial interface IUpdatePetEndpoint
{
    /// <summary>
    /// Update an existing pet by Id
    /// </summary>
    [Put("/pet")]
    Task<Pet> Execute([Body] Pet body);
}

/// <summary>
/// Add a new pet to the store
/// </summary>
public partial interface IAddPetEndpoint
{
    /// <summary>
    /// Add a new pet to the store
    /// </summary>
    [Post("/pet")]
    Task<Pet> Execute([Body] Pet body);
}

/// <summary>
/// Finds Pets by status
/// </summary>
public partial interface IFindPetsByStatusEndpoint
{
    /// <summary>
    /// Multiple status values can be provided with comma separated strings
    /// </summary>
    [Get("/pet/findByStatus")]
    Task<ICollection<Pet>> Execute([Query] Status? status);
}

/// <summary>
/// Finds Pets by tags
/// </summary>
public partial interface IFindPetsByTagsEndpoint
{
    /// <summary>
    /// Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.
    /// </summary>
    [Get("/pet/findByTags")]
    Task<ICollection<Pet>> Execute([Query(CollectionFormat.Multi)] IEnumerable<string> tags);
}

/// <summary>
/// Find pet by ID
/// </summary>
public partial interface IGetPetByIdEndpoint
{
    /// <summary>
    /// Returns a single pet
    /// </summary>
    [Get("/pet/{petId}")]
    Task<Pet> Execute(long petId);
}

/// <summary>
/// Updates a pet in the store with form data
/// </summary>
public partial interface IUpdatePetWithFormEndpoint
{
    [Post("/pet/{petId}")]
    Task Execute(long petId, [Query] string name, [Query] string status);
}

/// <summary>
/// Deletes a pet
/// </summary>
public partial interface IDeletePetEndpoint
{
    [Delete("/pet/{petId}")]
    Task Execute(long petId, [Header("api_key")] string api_key);
}

/// <summary>
/// uploads an image
/// </summary>
public partial interface IUploadFileEndpoint
{
    [Post("/pet/{petId}/uploadImage")]
    Task<ApiResponse> Execute(long petId, [Query] string additionalMetadata, StreamPart body);
}

/// <summary>
/// Returns pet inventories by status
/// </summary>
public partial interface IGetInventoryEndpoint
{
    /// <summary>
    /// Returns a map of status codes to quantities
    /// </summary>
    [Get("/store/inventory")]
    Task<IDictionary<string, int>> Execute();
}

/// <summary>
/// Place an order for a pet
/// </summary>
public partial interface IPlaceOrderEndpoint
{
    /// <summary>
    /// Place a new order in the store
    /// </summary>
    [Post("/store/order")]
    Task<Order> Execute([Body] Order body);
}

/// <summary>
/// Find purchase order by ID
/// </summary>
public partial interface IGetOrderByIdEndpoint
{
    /// <summary>
    /// For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions
    /// </summary>
    [Get("/store/order/{orderId}")]
    Task<Order> Execute(long orderId);
}

/// <summary>
/// Delete purchase order by ID
/// </summary>
public partial interface IDeleteOrderEndpoint
{
    /// <summary>
    /// For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors
    /// </summary>
    [Delete("/store/order/{orderId}")]
    Task Execute(long orderId);
}

/// <summary>
/// Create user
/// </summary>
public partial interface ICreateUserEndpoint
{
    /// <summary>
    /// This can only be done by the logged in user.
    /// </summary>
    [Post("/user")]
    Task Execute([Body] User body);
}

/// <summary>
/// Creates list of users with given input array
/// </summary>
public partial interface ICreateUsersWithListInputEndpoint
{
    /// <summary>
    /// Creates list of users with given input array
    /// </summary>
    [Post("/user/createWithList")]
    Task<User> Execute([Body] IEnumerable<User> body);
}

/// <summary>
/// Logs user into the system
/// </summary>
public partial interface ILoginUserEndpoint
{
    [Get("/user/login")]
    Task<string> Execute([Query] string username, [Query] string password);
}

/// <summary>
/// Logs out current logged in user session
/// </summary>
public partial interface ILogoutUserEndpoint
{
    [Get("/user/logout")]
    Task Execute();
}

/// <summary>
/// Get user by user name
/// </summary>
public partial interface IGetUserByNameEndpoint
{
    [Get("/user/{username}")]
    Task<User> Execute(string username);
}

/// <summary>
/// Update user
/// </summary>
public partial interface IUpdateUserEndpoint
{
    /// <summary>
    /// This can only be done by the logged in user.
    /// </summary>
    [Put("/user/{username}")]
    Task Execute(string username, [Body] User body);
}

/// <summary>
/// Delete user
/// </summary>
public partial interface IDeleteUserEndpoint
{
    /// <summary>
    /// This can only be done by the logged in user.
    /// </summary>
    [Delete("/user/{username}")]
    Task Execute(string username);
}
```

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