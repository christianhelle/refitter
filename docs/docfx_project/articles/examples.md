## Examples

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
        /// <summary>Update an existing pet</summary>
        /// <remarks>Update an existing pet by Id</remarks>
        /// <param name="body">Update an existent pet in the store</param>
        /// <returns>Successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid ID supplied
        /// 404: Pet not found
        /// 405: Validation exception
        /// </throws>
        [Headers("Accept: application/xml, application/json")]
        [Put("/pet")]
        Task<Pet> UpdatePet([Body] Pet body);

        /// <summary>Add a new pet to the store</summary>
        /// <remarks>Add a new pet to the store</remarks>
        /// <param name="body">Create a new pet in the store</param>
        /// <returns>Successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 405: Invalid input
        /// </throws>
        [Headers("Accept: application/xml, application/json")]
        [Post("/pet")]
        Task<Pet> AddPet([Body] Pet body);

        /// <summary>Finds Pets by status</summary>
        /// <remarks>Multiple status values can be provided with comma separated strings</remarks>
        /// <param name="status">Status values that need to be considered for filter</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid status value
        /// </throws>
        [Headers("Accept: application/json")]
        [Get("/pet/findByStatus")]
        Task<IList<Pet>> FindPetsByStatus([Query] Status? status);

        /// <summary>Finds Pets by tags</summary>
        /// <remarks>Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.</remarks>
        /// <param name="tags">Tags to filter by</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid tag value
        /// </throws>
        [Headers("Accept: application/json")]
        [Get("/pet/findByTags")]
        Task<IList<Pet>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

        /// <summary>Find pet by ID</summary>
        /// <remarks>Returns a single pet</remarks>
        /// <param name="petId">ID of pet to return</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid ID supplied
        /// 404: Pet not found
        /// </throws>
        [Headers("Accept: application/xml, application/json")]
        [Get("/pet/{petId}")]
        Task<Pet> GetPetById(long petId);

        /// <summary>Updates a pet in the store with form data</summary>
        /// <param name="petId">ID of pet that needs to be updated</param>
        /// <param name="name">Name of pet that needs to be updated</param>
        /// <param name="status">Status of pet that needs to be updated</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 405: Invalid input
        /// </throws>
        [Post("/pet/{petId}")]
        Task UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

        /// <summary>Deletes a pet</summary>
        /// <param name="petId">Pet id to delete</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid pet value
        /// </throws>
        [Delete("/pet/{petId}")]
        Task DeletePet(long petId, [Header("api_key")] string api_key);

        /// <summary>uploads an image</summary>
        /// <param name="petId">ID of pet to update</param>
        /// <param name="additionalMetadata">Additional Metadata</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Headers("Accept: application/json")]
        [Post("/pet/{petId}/uploadImage")]
        Task<ApiResponse> UploadFile(long petId, [Query] string additionalMetadata, StreamPart body);

        /// <summary>Returns pet inventories by status</summary>
        /// <remarks>Returns a map of status codes to quantities</remarks>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Headers("Accept: application/json")]
        [Get("/store/inventory")]
        Task<IDictionary<string, int>> GetInventory();

        /// <summary>Place an order for a pet</summary>
        /// <remarks>Place a new order in the store</remarks>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 405: Invalid input
        /// </throws>
        [Headers("Accept: application/json")]
        [Post("/store/order")]
        Task<Order> PlaceOrder([Body] Order body);

        /// <summary>Find purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions</remarks>
        /// <param name="orderId">ID of order that needs to be fetched</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid ID supplied
        /// 404: Order not found
        /// </throws>
        [Headers("Accept: application/json")]
        [Get("/store/order/{orderId}")]
        Task<Order> GetOrderById(long orderId);

        /// <summary>Delete purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors</remarks>
        /// <param name="orderId">ID of the order that needs to be deleted</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid ID supplied
        /// 404: Order not found
        /// </throws>
        [Delete("/store/order/{orderId}")]
        Task DeleteOrder(long orderId);

        /// <summary>Create user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="body">Created user object</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Headers("Accept: application/json, application/xml")]
        [Post("/user")]
        Task CreateUser([Body] User body);

        /// <summary>Creates list of users with given input array</summary>
        /// <remarks>Creates list of users with given input array</remarks>
        /// <returns>Successful operation</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Headers("Accept: application/xml, application/json")]
        [Post("/user/createWithList")]
        Task<User> CreateUsersWithListInput([Body] IEnumerable<User> body);

        /// <summary>Logs user into the system</summary>
        /// <param name="username">The user name for login</param>
        /// <param name="password">The password for login in clear text</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid username/password supplied
        /// </throws>
        [Headers("Accept: application/json")]
        [Get("/user/login")]
        Task<string> LoginUser([Query] string username, [Query] string password);

        /// <summary>Logs out current logged in user session</summary>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Get("/user/logout")]
        Task LogoutUser();

        /// <summary>Get user by user name</summary>
        /// <param name="username">The name that needs to be fetched. Use user1 for testing.</param>
        /// <returns>successful operation</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid username supplied
        /// 404: User not found
        /// </throws>
        [Headers("Accept: application/json")]
        [Get("/user/{username}")]
        Task<User> GetUserByName(string username);

        /// <summary>Update user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">name that need to be deleted</param>
        /// <param name="body">Update an existent user in the store</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">Thrown when the request returns a non-success status code.</throws>
        [Put("/user/{username}")]
        Task UpdateUser(string username, [Body] User body);

        /// <summary>Delete user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">The name that needs to be deleted</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <throws cref="ApiException">
        /// Thrown when the request returns a non-success status code:
        /// 400: Invalid username supplied
        /// 404: User not found
        /// </throws>
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
        /// <summary>Update an existing pet</summary>
        /// <remarks>Update an existing pet by Id</remarks>
        /// <param name="body">Update an existent pet in the store</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: Successful operation
        /// 400: Invalid ID supplied
        /// 404: Pet not found
        /// 405: Validation exception
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Put("/pet")]
        Task<IApiResponse<Pet>> UpdatePet([Body] Pet body);

        /// <summary>Add a new pet to the store</summary>
        /// <remarks>Add a new pet to the store</remarks>
        /// <param name="body">Create a new pet in the store</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: Successful operation
        /// 405: Invalid input
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Post("/pet")]
        Task<IApiResponse<Pet>> AddPet([Body] Pet body);

        /// <summary>Finds Pets by status</summary>
        /// <remarks>Multiple status values can be provided with comma separated strings</remarks>
        /// <param name="status">Status values that need to be considered for filter</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid status value
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/pet/findByStatus")]
        Task<IApiResponse<IList<Pet>>> FindPetsByStatus([Query] Status? status);

        /// <summary>Finds Pets by tags</summary>
        /// <remarks>Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.</remarks>
        /// <param name="tags">Tags to filter by</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid tag value
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/pet/findByTags")]
        Task<IApiResponse<IList<Pet>>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

        /// <summary>Find pet by ID</summary>
        /// <remarks>Returns a single pet</remarks>
        /// <param name="petId">ID of pet to return</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid ID supplied
        /// 404: Pet not found
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Get("/pet/{petId}")]
        Task<IApiResponse<Pet>> GetPetById(long petId);

        /// <summary>Updates a pet in the store with form data</summary>
        /// <param name="petId">ID of pet that needs to be updated</param>
        /// <param name="name">Name of pet that needs to be updated</param>
        /// <param name="status">Status of pet that needs to be updated</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 405: Invalid input
        /// </returns>
        [Post("/pet/{petId}")]
        Task<IApiResponse> UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

        /// <summary>Deletes a pet</summary>
        /// <param name="petId">Pet id to delete</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 400: Invalid pet value
        /// </returns>
        [Delete("/pet/{petId}")]
        Task<IApiResponse> DeletePet(long petId, [Header("api_key")] string api_key);

        /// <summary>uploads an image</summary>
        /// <param name="petId">ID of pet to update</param>
        /// <param name="additionalMetadata">Additional Metadata</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// </returns>
        [Headers("Accept: application/json")]
        [Post("/pet/{petId}/uploadImage")]
        Task<IApiResponse<ApiResponse>> UploadFile(long petId, [Query] string additionalMetadata, StreamPart body);

        /// <summary>Returns pet inventories by status</summary>
        /// <remarks>Returns a map of status codes to quantities</remarks>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/store/inventory")]
        Task<IApiResponse<IDictionary<string, int>>> GetInventory();

        /// <summary>Place an order for a pet</summary>
        /// <remarks>Place a new order in the store</remarks>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 405: Invalid input
        /// </returns>
        [Headers("Accept: application/json")]
        [Post("/store/order")]
        Task<IApiResponse<Order>> PlaceOrder([Body] Order body);

        /// <summary>Find purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions</remarks>
        /// <param name="orderId">ID of order that needs to be fetched</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid ID supplied
        /// 404: Order not found
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/store/order/{orderId}")]
        Task<IApiResponse<Order>> GetOrderById(long orderId);

        /// <summary>Delete purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors</remarks>
        /// <param name="orderId">ID of the order that needs to be deleted</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 400: Invalid ID supplied
        /// 404: Order not found
        /// </returns>
        [Delete("/store/order/{orderId}")]
        Task<IApiResponse> DeleteOrder(long orderId);

        /// <summary>Create user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="body">Created user object</param>
        /// <returns>A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result.</returns>
        [Headers("Accept: application/json, application/xml")]
        [Post("/user")]
        Task<IApiResponse> CreateUser([Body] User body);

        /// <summary>Creates list of users with given input array</summary>
        /// <remarks>Creates list of users with given input array</remarks>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: Successful operation
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Post("/user/createWithList")]
        Task<IApiResponse<User>> CreateUsersWithListInput([Body] IEnumerable<User> body);

        /// <summary>Logs user into the system</summary>
        /// <param name="username">The user name for login</param>
        /// <param name="password">The password for login in clear text</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid username/password supplied
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/user/login")]
        Task<IApiResponse<string>> LoginUser([Query] string username, [Query] string password);

        /// <summary>Logs out current logged in user session</summary>
        /// <returns>A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result.</returns>
        [Get("/user/logout")]
        Task<IApiResponse> LogoutUser();

        /// <summary>Get user by user name</summary>
        /// <param name="username">The name that needs to be fetched. Use user1 for testing.</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 200: successful operation
        /// 400: Invalid username supplied
        /// 404: User not found
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/user/{username}")]
        Task<IApiResponse<User>> GetUserByName(string username);

        /// <summary>Update user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">name that need to be deleted</param>
        /// <param name="body">Update an existent user in the store</param>
        /// <returns>A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result.</returns>
        [Put("/user/{username}")]
        Task<IApiResponse> UpdateUser(string username, [Body] User body);

        /// <summary>Delete user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">The name that needs to be deleted</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// 400: Invalid username supplied
        /// 404: User not found
        /// </returns>
        [Delete("/user/{username}")]
        Task<IApiResponse> DeleteUser(string username);
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