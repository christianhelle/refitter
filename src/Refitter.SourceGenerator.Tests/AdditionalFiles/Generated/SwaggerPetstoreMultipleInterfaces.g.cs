﻿// <auto-generated>
//     This code was generated by Refitter.
// </auto-generated>


using Refit;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Refitter.Tests.AdditionalFiles.SingeInterface;

#nullable enable annotations

namespace Refitter.Tests.AdditionalFiles.ByEndpoint
{
    /// <summary>Update an existing pet</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IUpdatePetEndpoint
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
        [Headers("Content-Type: application/json")]
        [Put("/pet")]
        Task<Pet> Execute([Body] Pet body);
    }

    /// <summary>Add a new pet to the store</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IAddPetEndpoint
    {
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
        [Headers("Content-Type: application/json")]
        [Post("/pet")]
        Task<Pet> Execute([Body] Pet body);
    }

    /// <summary>Finds Pets by status</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IFindPetsByStatusEndpoint
    {
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
        [Get("/pet/findByStatus")]
        Task<ICollection<Pet>> Execute([Query] Status? status);
    }

    /// <summary>Finds Pets by tags</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IFindPetsByTagsEndpoint
    {
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
        [Get("/pet/findByTags")]
        Task<ICollection<Pet>> Execute([Query(CollectionFormat.Multi)] IEnumerable<string> tags);
    }

    /// <summary>Find pet by ID</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IGetPetByIdEndpoint
    {
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
        [Get("/pet/{petId}")]
        Task<Pet> Execute(long petId);
    }

    /// <summary>Updates a pet in the store with form data</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IUpdatePetWithFormEndpoint
    {
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
        Task Execute(long petId, [Query] string name, [Query] string status);
    }

    /// <summary>Deletes a pet</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IDeletePetEndpoint
    {
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
        Task Execute(long petId, [Header("api_key")] string api_key);
    }

    /// <summary>uploads an image</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IUploadFileEndpoint
    {
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
        [Headers("Content-Type: application/octet-stream")]
        [Post("/pet/{petId}/uploadImage")]
        Task<ApiResponse> Execute(long petId, [Query] string additionalMetadata, StreamPart body);
    }

    /// <summary>Returns pet inventories by status</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IGetInventoryEndpoint
    {
        /// <summary>Returns pet inventories by status</summary>
        /// <remarks>Returns a map of status codes to quantities</remarks>
        /// <returns>successful operation</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Get("/store/inventory")]
        Task<IDictionary<string, int>> Execute();
    }

    /// <summary>Place an order for a pet</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IPlaceOrderEndpoint
    {
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
        [Headers("Content-Type: application/json")]
        [Post("/store/order")]
        Task<Order> Execute([Body] Order body);
    }

    /// <summary>Find purchase order by ID</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IGetOrderByIdEndpoint
    {
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
        [Get("/store/order/{orderId}")]
        Task<Order> Execute(long orderId);
    }

    /// <summary>Delete purchase order by ID</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IDeleteOrderEndpoint
    {
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
        Task Execute(long orderId);
    }

    /// <summary>Create user</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ICreateUserEndpoint
    {
        /// <summary>Create user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="body">Created user object</param>
        /// <returns>successful operation</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Headers("Content-Type: application/json")]
        [Post("/user")]
        Task Execute([Body] User body);
    }

    /// <summary>Creates list of users with given input array</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ICreateUsersWithListInputEndpoint
    {
        /// <summary>Creates list of users with given input array</summary>
        /// <remarks>Creates list of users with given input array</remarks>
        /// <returns>Successful operation</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Headers("Content-Type: application/json")]
        [Post("/user/createWithList")]
        Task<User> Execute([Body] IEnumerable<User> body);
    }

    /// <summary>Logs user into the system</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ILoginUserEndpoint
    {
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
        [Get("/user/login")]
        Task<string> Execute([Query] string username, [Query] string password);
    }

    /// <summary>Logs out current logged in user session</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ILogoutUserEndpoint
    {
        /// <summary>Logs out current logged in user session</summary>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Get("/user/logout")]
        Task Execute();
    }

    /// <summary>Get user by user name</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IGetUserByNameEndpoint
    {
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
        [Get("/user/{username}")]
        Task<User> Execute(string username);
    }

    /// <summary>Update user</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IUpdateUserEndpoint
    {
        /// <summary>Update user</summary>
        /// <remarks>This can only be done by the logged in user.</remarks>
        /// <param name="username">name that need to be deleted</param>
        /// <param name="body">Update an existent user in the store</param>
        /// <returns>A <see cref="Task"/> that completes when the request is finished.</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Headers("Content-Type: application/json")]
        [Put("/user/{username}")]
        Task Execute(string username, [Body] User body);
    }

    /// <summary>Delete user</summary>
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface IDeleteUserEndpoint
    {
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
        Task Execute(string username);
    }

}




#nullable enable
namespace Refitter.Tests.AdditionalFiles.ByEndpoint
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Refit;

    public static partial class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRefitClients(
            this IServiceCollection services, 
            Uri baseUrl, 
            Action<IHttpClientBuilder>? builder = default, 
            RefitSettings? settings = default)
        {
            var clientBuilderIUpdatePetEndpoint = services
                .AddRefitClient<IUpdatePetEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIUpdatePetEndpoint);

            var clientBuilderIAddPetEndpoint = services
                .AddRefitClient<IAddPetEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIAddPetEndpoint);

            var clientBuilderIFindPetsByStatusEndpoint = services
                .AddRefitClient<IFindPetsByStatusEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIFindPetsByStatusEndpoint);

            var clientBuilderIFindPetsByTagsEndpoint = services
                .AddRefitClient<IFindPetsByTagsEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIFindPetsByTagsEndpoint);

            var clientBuilderIGetPetByIdEndpoint = services
                .AddRefitClient<IGetPetByIdEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIGetPetByIdEndpoint);

            var clientBuilderIUpdatePetWithFormEndpoint = services
                .AddRefitClient<IUpdatePetWithFormEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIUpdatePetWithFormEndpoint);

            var clientBuilderIDeletePetEndpoint = services
                .AddRefitClient<IDeletePetEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIDeletePetEndpoint);

            var clientBuilderIUploadFileEndpoint = services
                .AddRefitClient<IUploadFileEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIUploadFileEndpoint);

            var clientBuilderIGetInventoryEndpoint = services
                .AddRefitClient<IGetInventoryEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIGetInventoryEndpoint);

            var clientBuilderIPlaceOrderEndpoint = services
                .AddRefitClient<IPlaceOrderEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIPlaceOrderEndpoint);

            var clientBuilderIGetOrderByIdEndpoint = services
                .AddRefitClient<IGetOrderByIdEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIGetOrderByIdEndpoint);

            var clientBuilderIDeleteOrderEndpoint = services
                .AddRefitClient<IDeleteOrderEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIDeleteOrderEndpoint);

            var clientBuilderICreateUserEndpoint = services
                .AddRefitClient<ICreateUserEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderICreateUserEndpoint);

            var clientBuilderICreateUsersWithListInputEndpoint = services
                .AddRefitClient<ICreateUsersWithListInputEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderICreateUsersWithListInputEndpoint);

            var clientBuilderILoginUserEndpoint = services
                .AddRefitClient<ILoginUserEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderILoginUserEndpoint);

            var clientBuilderILogoutUserEndpoint = services
                .AddRefitClient<ILogoutUserEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderILogoutUserEndpoint);

            var clientBuilderIGetUserByNameEndpoint = services
                .AddRefitClient<IGetUserByNameEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIGetUserByNameEndpoint);

            var clientBuilderIUpdateUserEndpoint = services
                .AddRefitClient<IUpdateUserEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIUpdateUserEndpoint);

            var clientBuilderIDeleteUserEndpoint = services
                .AddRefitClient<IDeleteUserEndpoint>(settings)
                .ConfigureHttpClient(c => c.BaseAddress = baseUrl)
                .AddHttpMessageHandler<EmptyMessageHandler>()
                .AddHttpMessageHandler<AnotherEmptyMessageHandler>();

            builder?.Invoke(clientBuilderIDeleteUserEndpoint);

            return services;
        }
    }
}