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

## Dependency Injection

Refitter supports generating bootstrapping code that allows the user to conveniently configure all generated Refit interfaces by calling a single extension method to `IServiceCollection`.

This is enabled through the `.refitter` settings file like this:

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "TelemetryDelegatingHandler" ],
    "transientErrorHandler": "Polly",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  }
}
```

which will generate an extension method to `IServiceCollection` called `ConfigureRefitClients()`. The generated extension method depends on [`Refit.HttpClientFactory`](https://www.nuget.org/packages/Refit.HttpClientFactory) library and looks like this:

```cs
public static IServiceCollection ConfigureRefitClients(
    this IServiceCollection services, 
    Action<IHttpClientBuilder>? builder = default, 
    RefitSettings? settings = default)
{
    var clientBuilderISwaggerPetstore = services
        .AddRefitClient<ISwaggerPetstore>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderISwaggerPetstore
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderISwaggerPetstore);

    return services;
}
```

This comes in handy especially when generating multiple interfaces, by tag or endpoint. For example, the following `.refitter` settings file

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "multipleInterfaces": "ByTag",
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "TelemetryDelegatingHandler" ],
    "transientErrorHandler": "Polly",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  }
}
```

Will generate a single `ConfigureRefitClients()` extension methods that may contain dependency injection configuration code for multiple interfaces like this

```csharp
public static IServiceCollection ConfigureRefitClients(
    this IServiceCollection services, 
    Action<IHttpClientBuilder>? builder = default, 
    RefitSettings? settings = default)
{
    var clientBuilderIPetApi = services
        .AddRefitClient<IPetApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIPetApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIPetApi);

    var clientBuilderIStoreApi = services
        .AddRefitClient<IStoreApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIStoreApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIStoreApi);

    var clientBuilderIUserApi = services
        .AddRefitClient<IUserApi>(settings)
        .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://petstore3.swagger.io/api/v3"))
        .AddHttpMessageHandler<TelemetryDelegatingHandler>();

    clientBuilderIUserApi
        .AddPolicyHandler(
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(
                        TimeSpan.FromSeconds(0.5),
                        3)));

    builder?.Invoke(clientBuilderIUserApi);

    return services;
}
```

Personally, they I use Refitter is to generate an interface per endpoint, so when generating code for a large and complex API, I might have several interfaces.

## Apizr

[Apizr](https://www.apizr.net) is a Refit client manager that provides a set of features to enhance requesting experience with resilience, caching, priority, mediation, mapping, logging, authentication, file transfer capabilities and many more...

### Generating the interfaces

Refitter supports generating Apizr formatted Refit interfaces that can be managed then by Apizr (v6+).

You can enable Apizr formatted Refit interface generation either:
- With the `--use-apizr` command line argument
- By setting the `apizrSettings` section in the `.refitter` settings file

Note that `--use-apizr` uses default Apizr settings with `withRequestOptions` set to `true` as recommended, while the `.refitter` settings file allows you to configure it deeper.

In both cases, it will format the generated Refit interfaces to be Apizr ready by:
- Adding a final IApizrRequestOptions options parameter to all generated methods (if `withRequestOptions` is set to `true`)
- Providing cancellation tokens by Apizr request options instead of a dedicated parameter (if `withRequestOptions` is set to `true`)
- Using method overloads instead of optional parameters (note that setting `useDynamicQuerystringParameters` to true improve overloading experience)

From here, you're definitly free to use the formatted interface with Apizr by registering, configuring and using it following the [Apizr documentation](https://www.apizr.net). But Refitter can go further by generating some helpers to make the configuration easier.

### Generating the helpers

Refitter supports generating Apizr (v6+) bootstrapping code that allows the user to conveniently configure all generated Apizr formatted Refit interfaces by calling a single method.
It could be either an extension method to `IServiceCollection` if DependencyInjectionSettings are set, or a static builder method if not.

### [Extended](#tab/tabid-extended)

To enable Apizr registration code generation for `IServiceCollection`, you need at least to set the `withRegistrationHelper` property to `true` and configure the `DependencyInjectionSettings` section in the `.refitter` settings file.
This is what the `.refitter` settings file may look like, depending on you configuration:

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "useDynamicQuerystringParameters": true,
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "MyDelegatingHandler" ],
    "transientErrorHandler": "HttpResilience",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  },
  "apizrSettings": {
    "withRequestOptions": true, // Recommended to include an Apizr request options parameter to Refit interface methods
    "withRegistrationHelper": true, // Mandatory to actually generate the Apizr registration extended method
    "withCacheProvider": "InMemory", // Optional, default is None
    "withPriority": true, // Optional, default is false
    "withMediation": true, // Optional, default is false
    "withOptionalMediation": true, // Optional, default is false
    "withMappingProvider": "AutoMapper", // Optional, default is None
    "withFileTransfer": true // Optional, default is false
  }
}
```

which will generate an extension method to `IServiceCollection` called `ConfigurePetstoreApiApizrManager()`. The generated extension method depends on [`Apizr.Extensions.Microsoft.DependencyInjection`](https://www.nuget.org/packages/Apizr.Extensions.Microsoft.DependencyInjection) library and looks like this:

```cs
public static IServiceCollection ConfigurePetstoreApiApizrManager(
    this IServiceCollection services,
    Action<IApizrExtendedManagerOptionsBuilder>? optionsBuilder = null)
{
    optionsBuilder ??= _ => { }; // Default empty options if null
    optionsBuilder += options => options
        .WithBaseAddress("https://petstore3.swagger.io/api/v3", ApizrDuplicateStrategy.Ignore)
        .WithDelegatingHandler<MyDelegatingHandler>()
        .ConfigureHttpClientBuilder(builder => builder
            .AddStandardResilienceHandler(config =>
            {
                config.Retry = new HttpRetryStrategyOptions
                {
                    UseJitter = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(0.5)
                };
            }))
        .WithInMemoryCacheHandler()
        .WithAutoMapperMappingHandler()
        .WithPriority()
        .WithOptionalMediation()
        .WithFileTransferOptionalMediation();
                 
    return services.AddApizrManagerFor<IPetstoreApi>(optionsBuilder);
}
```

This comes in handy especially when generating multiple interfaces, by tag or endpoint. For example, the following `.refitter` settings file

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "useDynamicQuerystringParameters": true,
  "multipleInterfaces": "ByTag",
  "naming": {    
    "useOpenApiTitle": false,
    "interfaceName": "Petstore"
  },
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "MyDelegatingHandler" ],
    "transientErrorHandler": "HttpResilience",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  },
  "apizrSettings": {
    "withRequestOptions": true, // Recommended to include an Apizr request options parameter to Refit interface methods
    "withRegistrationHelper": true, // Mandatory to actually generate the Apizr registration extended method
    "withCacheProvider": "InMemory", // Optional, default is None
    "withPriority": true, // Optional, default is false
    "withMediation": true, // Optional, default is false
    "withOptionalMediation": true, // Optional, default is false
    "withMappingProvider": "AutoMapper", // Optional, default is None
    "withFileTransfer": true // Optional, default is false
  }
}
```

Will generate a single `ConfigurePetstoreApizrManagers()` extension method that may contain dependency injection configuration code for multiple interfaces like this

```csharp
public static IServiceCollection ConfigurePetstoreApizrManagers(
    this IServiceCollection services,
    Action<IApizrExtendedCommonOptionsBuilder>? optionsBuilder = null)
{
    optionsBuilder ??= _ => { }; // Default empty options if null
    optionsBuilder += options => options
        .WithBaseAddress("https://petstore3.swagger.io/api/v3", ApizrDuplicateStrategy.Ignore)
        .WithDelegatingHandler<MyDelegatingHandler>()
        .ConfigureHttpClientBuilder(builder => builder
            .AddStandardResilienceHandler(config =>
            {
                config.Retry = new HttpRetryStrategyOptions
                {
                    UseJitter = true,
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(0.5)
                };
            }))
        .WithInMemoryCacheHandler()
        .WithAutoMapperMappingHandler()
        .WithPriority()
        .WithOptionalMediation()
        .WithFileTransferOptionalMediation();
            
    return services.AddApizr(
        registry => registry
            .AddManagerFor<IPetApi>()
            .AddManagerFor<IStoreApi>()
            .AddManagerFor<IUserApi>(),
        optionsBuilder);

}
```

Here, `IPetApi`, `IStoreApi` and `IUserApi` are the generated interfaces which share the same common configuration defined from the `.refitter` file.

### [Static](#tab/tabid-static)

To enable Apizr static builder code generation, you need at least to set the `withRegistrationHelper` property to `true` and leave the `DependencyInjectionSettings` section to null in the `.refitter` settings file.
This is what the `.refitter` settings file may look like, depending on you configuration:

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "useDynamicQuerystringParameters": true,
  "apizrSettings": {
    "withRequestOptions": true, // Recommended to include an Apizr request options parameter to Refit interface methods
    "withRegistrationHelper": true, // Mandatory to actually generate the Apizr registration extended method
    "withCacheProvider": "Akavache", // Optional, default is None
    "withPriority": true, // Optional, default is false
    "withMappingProvider": "AutoMapper", // Optional, default is None
    "withFileTransfer": true // Optional, default is false
  }
}
```

which will generate a static builder method called `BuildPetstore30ApizrManager()`. The generated builder method depends on [`Apizr`](https://www.nuget.org/packages/Apizr) library and looks like this:

```cs
public static IApizrManager<ISwaggerPetstoreOpenAPI30> BuildPetstore30ApizrManager(Action<IApizrManagerOptionsBuilder> optionsBuilder)
{
    optionsBuilder ??= _ => { }; // Default empty options if null
    optionsBuilder += options => options
        .WithAkavacheCacheHandler()
        .WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))
        .WithPriority();
            
    return ApizrBuilder.Current.CreateManagerFor<ISwaggerPetstoreOpenAPI30>(optionsBuilder);  
}
```

This comes in handy especially when generating multiple interfaces, by tag or endpoint. For example, the following `.refitter` settings file

```json
{
  "openApiPath": "../OpenAPI/v3.0/petstore.json",
  "namespace": "Petstore",
  "multipleInterfaces": "ByTag",
  "naming": {    
    "useOpenApiTitle": false,
    "interfaceName": "Petstore"
  },
  "dependencyInjectionSettings": {
    "baseUrl": "https://petstore3.swagger.io/api/v3",
    "httpMessageHandlers": [ "MyDelegatingHandler" ],
    "transientErrorHandler": "HttpResilience",
    "maxRetryCount": 3,
    "firstBackoffRetryInSeconds": 0.5
  },
  "apizrSettings": {
    "withRequestOptions": true, // Recommended to include an Apizr request options parameter to Refit interface methods
    "withRegistrationHelper": true, // Mandatory to actually generate the Apizr registration extended method
    "withCacheProvider": "InMemory", // Optional, default is None
    "withPriority": true, // Optional, default is false
    "withMediation": true, // Optional, default is false
    "withOptionalMediation": true, // Optional, default is false
    "withMappingProvider": "AutoMapper", // Optional, default is None
    "withFileTransfer": true // Optional, default is false
  }
}
```

Will generate a single `BuildPetstoreApizrManagers()` builder method that may contain configuration code for multiple interfaces like this

```csharp
public static IApizrRegistry BuildPetstoreApizrManagers(Action<IApizrCommonOptionsBuilder> optionsBuilder)
{
    optionsBuilder ??= _ => { }; // Default empty options if null
    optionsBuilder += options => options
        .WithAkavacheCacheHandler()
        .WithAutoMapperMappingHandler(new MapperConfiguration(config => { /* YOUR_MAPPINGS_HERE */ }))
        .WithPriority();
            
    return ApizrBuilder.Current.CreateRegistry(
        registry => registry
            .AddManagerFor<IPetApi>()
            .AddManagerFor<IStoreApi>()
            .AddManagerFor<IUserApi>(),
        optionsBuilder);
}
```

Here, `IPetApi`, `IStoreApi` and `IUserApi` are the generated interfaces which share the same common configuration defined from the `.refitter` file.

***

### Customizing the configuration

You may want to adjust apis configuration, for example, to add a custom header to requests. This can be done using the `Action<TApizrOptionsBuilder>` parameter while calling the generated method.
To know how to make Apizr fit your needs, please refer to the [Apizr documentation](https://www.apizr.net).

### Using the managers

Once you called the generated method, you will get an `IApizrManager<T>` instance that you can use to make requests to the API. Here's an example of how to use it:

```csharp
var result = await petstoreManager.ExecuteAsync((api, opt) => api.GetPetById(1, opt), 
    options => options // Whatever final request options you want to apply
        .WithPriority(Priority.Background)
        .WithHeaders(["HeaderKey1: HeaderValue1"])
        .WithRequestTimeout("00:00:10")
        .WithCancellation(cts.Token));
```

Please head to the [Apizr documentation](https://www.apizr.net) to get more.

### Using the polymorphic serialization

NSwag provides built-in support for inheritance serialization, which is enabled by default. To leverage the native inheritance serialization support from System.Text.Json, enable the ```use-polymorphic-serialization``` setting. This enhances serialization and deserialization performance while maintaining compatibility by falling back to base types when needed.

Below are code examples with and without this setting enabled. The generated code is nearly identical, but with ```use-polymorphic-serialization``` enabled, deserialization attempts to instantiate the correct derived type wherever Tool instances are exposed. If an unknown type is encountered in the ```$type``` field, it will safely default to the base type.

Additionally, this setting addresses a known issue in NSwag, preventing a ```StackOverflowException``` when unknown types are encountered.

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType, IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(TextTool), typeDiscriminator: "TextTool")]
[JsonDerivedType(typeof(BrushTool), typeDiscriminator: "BrushTool")]
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Tool
{
}
```

```csharp
[JsonInheritanceConverter(typeof(SomeComponent), "$type")]
[JsonInheritanceAttribute("TextTool", typeof(TextTool))]
[JsonInheritanceAttribute("BrushTool", typeof(BrushTool))]
[System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "14.1.0.0 (NJsonSchema v11.0.2.0 (Newtonsoft.Json v13.0.0.0))")]
public partial class Tool
{
}
```

See 
- [System.Text.Json.Serialization documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism)
- [Add support for STJ-native C# code gen](https://github.com/RicoSuter/NJsonSchema/pull/1675)
- [Stackoverflow Exception](https://github.com/RicoSuter/NJsonSchema/issues/1728)
for more information