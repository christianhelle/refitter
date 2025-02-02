﻿// <auto-generated>
//     This code was generated by Refitter.
// </auto-generated>


using Refit;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

#nullable enable annotations

namespace Refitter.Tests.AdditionalFiles.SingeInterface
{
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ISwaggerPetstoreInterface
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
        [Headers("Accept: application/json")]
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
        [Headers("Accept: application/json")]
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
        /// <param name="name">Name of pet that needs to be updated</param>
        /// <param name="status">Status of pet that needs to be updated</param>
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
        Task UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

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
        Task<ApiResponse> UploadFile(long petId, [Query] string additionalMetadata, StreamPart body);

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
        /// <remarks>For valid response try integer IDs with value &lt;= 5 or &gt; 10. Other values will generated exceptions</remarks>
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
        [Headers("Accept: application/json")]
        [Get("/store/order/{orderId}")]
        Task<Order> GetOrderById(long orderId);

        /// <summary>Delete purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value &lt; 1000. Anything above 1000 or nonintegers will generate API errors</remarks>
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
        /// <param name="username">The user name for login</param>
        /// <param name="password">The password for login in clear text</param>
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
        [Headers("Accept: application/json")]
        [Get("/user/login")]
        Task<string> LoginUser([Query] string username, [Query] string password);

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
        [Headers("Accept: application/json")]
        [Get("/user/{username}")]
        Task<User> GetUserByName(string username);

        /// <summary>Update user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">name that need to be deleted</param>
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

}

//----------------------
// <auto-generated>
//     Generated using the NSwag toolchain v14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0)) (http://NSwag.org)
// </auto-generated>
//----------------------

#pragma warning disable 108 // Disable "CS0108 '{derivedDto}.ToJson()' hides inherited member '{dtoBase}.ToJson()'. Use the new keyword if hiding was intended."
#pragma warning disable 114 // Disable "CS0114 '{derivedDto}.RaisePropertyChanged(String)' hides inherited member 'dtoBase.RaisePropertyChanged(String)'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword."
#pragma warning disable 472 // Disable "CS0472 The result of the expression is always 'false' since a value of type 'Int32' is never equal to 'null' of type 'Int32?'
#pragma warning disable 612 // Disable "CS0612 '...' is obsolete"
#pragma warning disable 649 // Disable "CS0649 Field is never assigned to, and will always have its default value null"
#pragma warning disable 1573 // Disable "CS1573 Parameter '...' has no matching param tag in the XML comment for ...
#pragma warning disable 1591 // Disable "CS1591 Missing XML comment for publicly visible type or member ..."
#pragma warning disable 8073 // Disable "CS8073 The result of the expression is always 'false' since a value of type 'T' is never equal to 'null' of type 'T?'"
#pragma warning disable 3016 // Disable "CS3016 Arrays as attribute arguments is not CLS-compliant"
#pragma warning disable 8603 // Disable "CS8603 Possible null reference return"
#pragma warning disable 8604 // Disable "CS8604 Possible null reference argument for parameter"
#pragma warning disable 8625 // Disable "CS8625 Cannot convert null literal to non-nullable reference type"
#pragma warning disable 8765 // Disable "CS8765 Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes)."

namespace Refitter.Tests.AdditionalFiles.SingeInterface
{
    using System = global::System;

    

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Order
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("petId")]
        public long PetId { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("shipDate")]
        public System.DateTimeOffset ShipDate { get; set; }

        /// <summary>
        /// Order Status
        /// </summary>

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OrderStatus Status { get; set; }

        [JsonPropertyName("complete")]
        public bool Complete { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Customer
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("address")]
        public ICollection<Address> Address { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Address
    {

        [JsonPropertyName("street")]
        public string Street { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("zip")]
        public string Zip { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Category
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class User
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        /// <summary>
        /// User Status
        /// </summary>

        [JsonPropertyName("userStatus")]
        public int UserStatus { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Tag
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class Pet
    {

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public Category Category { get; set; }

        [JsonPropertyName("photoUrls")]
        [System.ComponentModel.DataAnnotations.Required]
        public ICollection<string> PhotoUrls { get; set; } = new System.Collections.ObjectModel.Collection<string>();

        [JsonPropertyName("tags")]
        public ICollection<Tag> Tags { get; set; }

        /// <summary>
        /// pet status in the store
        /// </summary>

        [JsonPropertyName("status")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PetStatus Status { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class ApiResponse
    {

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        private IDictionary<string, object> _additionalProperties;

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public enum Status
    {

        [System.Runtime.Serialization.EnumMember(Value = @"available")]
        Available = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"pending")]
        Pending = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"sold")]
        Sold = 2,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public enum OrderStatus
    {

        [System.Runtime.Serialization.EnumMember(Value = @"placed")]
        Placed = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"approved")]
        Approved = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"delivered")]
        Delivered = 2,

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public enum PetStatus
    {

        [System.Runtime.Serialization.EnumMember(Value = @"available")]
        Available = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"pending")]
        Pending = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"sold")]
        Sold = 2,

    }

    [System.CodeDom.Compiler.GeneratedCode("NSwag", "14.2.0.0 (NJsonSchema v11.1.0.0 (Newtonsoft.Json v13.0.0.0))")]
    public partial class FileParameter
    {
        public FileParameter(System.IO.Stream data)
            : this (data, null, null)
        {
        }

        public FileParameter(System.IO.Stream data, string fileName)
            : this (data, fileName, null)
        {
        }

        public FileParameter(System.IO.Stream data, string fileName, string contentType)
        {
            Data = data;
            FileName = fileName;
            ContentType = contentType;
        }

        public System.IO.Stream Data { get; private set; }

        public string FileName { get; private set; }

        public string ContentType { get; private set; }
    }


}

#pragma warning restore  108
#pragma warning restore  114
#pragma warning restore  472
#pragma warning restore  612
#pragma warning restore 1573
#pragma warning restore 1591
#pragma warning restore 8073
#pragma warning restore 3016
#pragma warning restore 8603
#pragma warning restore 8604
#pragma warning restore 8625


#nullable enable
namespace Refitter.Tests.AdditionalFiles.SingeInterface
{
    using System;
   using Microsoft.Extensions.DependencyInjection;
   using Polly;
   using Polly.Contrib.WaitAndRetry;
   using Polly.Extensions.Http;
   using Refit;

    public static partial class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRefitClients(
            this IServiceCollection services, 
            Action<IHttpClientBuilder>? builder = default, 
            RefitSettings? settings = default)
        {
            var clientBuilderISwaggerPetstoreInterface = services
                .AddRefitClient<ISwaggerPetstoreInterface>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            clientBuilderISwaggerPetstoreInterface
                .AddPolicyHandler(
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .WaitAndRetryAsync(
                            Backoff.DecorrelatedJitterBackoffV2(
                                TimeSpan.FromSeconds(0.5),
                                3)));

            builder?.Invoke(clientBuilderISwaggerPetstoreInterface);

            return services;
        }
    }
}