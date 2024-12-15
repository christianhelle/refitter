[System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
public partial interface ISwaggerPetstoreOpenAPI30
{
    /// <summary>Update an existing pet</summary>
    /// <remarks>Update an existing pet by Id</remarks>
    /// <param name="body">Update an existent pet in the store</param>
    /// <returns>Successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid ID supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>Pet not found</description>
    /// </item>
    /// <item>
    /// <term>405</term>
    /// <description>Validation exception</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Put("/pet")]
    Task<Pet> UpdatePet([Body] Pet body);

    /// <summary>Add a new pet to the store</summary>
    /// <remarks>Add a new pet to the store</remarks>
    /// <param name="body">Create a new pet in the store</param>
    /// <returns>Successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>405</term>
    /// <description>Invalid input</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Post("/pet")]
    Task<Pet> AddPet([Body] Pet body);

    /// <summary>Finds Pets by status</summary>
    /// <remarks>Multiple status values can be provided with comma separated strings</remarks>
    /// <param name="status">Status values that need to be considered for filter</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid status value</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/pet/findByStatus")]
    Task<ICollection<Pet>> FindPetsByStatus([Query] Status? status);

    /// <summary>Finds Pets by tags</summary>
    /// <remarks>Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.</remarks>
    /// <param name="tags">Tags to filter by</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid tag value</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/pet/findByTags")]
    Task<ICollection<Pet>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

    /// <summary>Find pet by ID</summary>
    /// <remarks>Returns a single pet</remarks>
    /// <param name="petId">ID of pet to return</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid ID supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>Pet not found</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/pet/{petId}")]
    Task<Pet> GetPetById(long petId);

    /// <summary>Updates a pet in the store with form data</summary>
    /// <param name="petId">ID of pet that needs to be updated</param>
    /// <param name="queryParams">The dynamic querystring parameter wrapping all others.</param>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>405</term>
    /// <description>Invalid input</description>
    /// </item>
    /// </list>
    /// </exception>
    [Post("/pet/{petId}")]
    Task UpdatePetWithForm(long petId, [Query] UpdatePetWithFormQueryParams queryParams);

    /// <summary>Deletes a pet</summary>
    /// <param name="petId">Pet id to delete</param>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid pet value</description>
    /// </item>
    /// </list>
    /// </exception>
    [Delete("/pet/{petId}")]
    Task DeletePet(long petId, [Header("api_key")] string api_key);

    /// <summary>uploads an image</summary>
    /// <param name="petId">ID of pet to update</param>
    /// <param name="additionalMetadata">Additional Metadata</param>
    /// <returns>
    /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>200</term>
    /// <description>successful operation</description>
    /// </item>
    /// </list>
    /// </returns>
    [Headers("Accept: application/json")]
    [Post("/pet/{petId}/uploadImage")]
    Task<ApiResponse> UploadFile(long petId, [Query] string additionalMetadata,  StreamPart body);

    /// <summary>Returns pet inventories by status</summary>
    /// <remarks>Returns a map of status codes to quantities</remarks>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: application/json")]
    [Get("/store/inventory")]
    Task<IDictionary<string, int>> GetInventory();

    /// <summary>Place an order for a pet</summary>
    /// <remarks>Place a new order in the store</remarks>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>405</term>
    /// <description>Invalid input</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/json")]
    [Post("/store/order")]
    Task<Order> PlaceOrder([Body] Order body);

    /// <summary>Find purchase order by ID</summary>
    /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generate exceptions.</remarks>
    /// <param name="orderId">ID of order that needs to be fetched</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid ID supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>Order not found</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/store/order/{orderId}")]
    Task<Order> GetOrderById(long orderId);

    /// <summary>Delete purchase order by ID</summary>
    /// <remarks>For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors</remarks>
    /// <param name="orderId">ID of the order that needs to be deleted</param>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid ID supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>Order not found</description>
    /// </item>
    /// </list>
    /// </exception>
    [Delete("/store/order/{orderId}")]
    Task DeleteOrder(long orderId);

    /// <summary>Create user</summary>
    /// <remarks>This can only be done by the logged in user.</remarks>
    /// <param name="body">Created user object</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: application/json, application/xml")]
    [Post("/user")]
    Task CreateUser([Body] User body);

    /// <summary>Creates list of users with given input array</summary>
    /// <remarks>Creates list of users with given input array</remarks>
    /// <returns>Successful operation</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: application/xml, application/json")]
    [Post("/user/createWithList")]
    Task<User> CreateUsersWithListInput([Body] IEnumerable<User> body);

    /// <summary>Logs user into the system</summary>
    /// <param name="queryParams">The dynamic querystring parameter wrapping all others.</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid username/password supplied</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/user/login")]
    Task<string> LoginUser([Query] LoginUserQueryParams queryParams);

    /// <summary>Logs out current logged in user session</summary>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Get("/user/logout")]
    Task LogoutUser();

    /// <summary>Get user by user name</summary>
    /// <param name="username">The name that needs to be fetched. Use user1 for testing.</param>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid username supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>User not found</description>
    /// </item>
    /// </list>
    /// </exception>
    [Headers("Accept: application/xml, application/json")]
    [Get("/user/{username}")]
    Task<User> GetUserByName(string username);

    /// <summary>Update user</summary>
    /// <remarks>This can only be done by the logged in user.</remarks>
    /// <param name="username">name that needs to be updated</param>
    /// <param name="body">Update an existent user in the store</param>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Put("/user/{username}")]
    Task UpdateUser(string username, [Body] User body);

    /// <summary>Delete user</summary>
    /// <remarks>This can only be done by the logged in user.</remarks>
    /// <param name="username">The name that needs to be deleted</param>
    /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
    /// <exception cref="ApiException">
    /// Thrown when the request returns a non-success status code:
    /// <list type="table">
    /// <listheader>
    /// <term>Status</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>400</term>
    /// <description>Invalid username supplied</description>
    /// </item>
    /// <item>
    /// <term>404</term>
    /// <description>User not found</description>
    /// </item>
    /// </list>
    /// </exception>
    [Delete("/user/{username}")]
    Task DeleteUser(string username);


}

public class UpdatePetWithFormQueryParams
{

    /// <summary>
    /// Name of pet that needs to be updated
    /// </summary>
    [Query]
    public string Name { get; set; }

    /// <summary>
    /// Status of pet that needs to be updated
    /// </summary>
    [Query]
    public string Status { get; set; }

}

public class LoginUserQueryParams
{

    /// <summary>
    /// The user name for login
    /// </summary>
    [Query]
    public string Username { get; set; }

    /// <summary>
    /// The password for login in clear text
    /// </summary>
    [Query]
    public string Password { get; set; }

}