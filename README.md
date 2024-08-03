[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)
[![Smoke Tests](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/smoke-tests.yml)
[![NuGet](https://img.shields.io/nuget/v/refitter?color=blue)](https://www.nuget.org/packages/refitter)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=christianhelle_refitter&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=christianhelle_refitter)
[![codecov](https://codecov.io/gh/christianhelle/refitter/graph/badge.svg?token=242YT1N6T2)](https://codecov.io/gh/christianhelle/refitter)

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-48-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

# Refitter
Refitter is a tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface and contracts from OpenAPI specifications. Refitter could format the generated Refit interface to be managed by [Apizr](https://www.apizr.net) (v6+) and generate some registration helpers too.

Refitter comes in 2 forms:
- A [.NET CLI Tool](#cli-tool) distributed via [nuget.org](http://www.nuget.org/packages/refitter) that outputs a single C# file on disk
- A [C# Source Generator](#source-generator) via the [Refitter.SourceGenerator](http://www.nuget.org/packages/refitter.sourcegenerator) package that generates code on compile time based on a [.refitter](#.refitter-file-format) within the project directory.

## CLI Tool

### Installation:

The tool is packaged as a .NET Tool and is published to nuget.org. You can install the latest version of this tool like this:

```shell
dotnet tool install --global Refitter
```

### Usage:

```shell
$ refitter --help
```

```text
USAGE:
    refitter [URL or input file] [OPTIONS]

EXAMPLES:
    refitter ./openapi.json
    refitter https://petstore3.swagger.io/api/v3/openapi.yaml
    refitter ./openapi.json --settings-file ./openapi.refitter --output ./GeneratedCode.cs
    refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode" --output ./GeneratedCode.cs
    refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode" --internal
    refitter ./openapi.json --output ./IGeneratedCode.cs --interface-only
    refitter ./openapi.json --use-api-response
    refitter ./openapi.json --cancellation-tokens
    refitter ./openapi.json --no-operation-headers
    refitter ./openapi.json --no-accept-headers
    refitter ./openapi.json --use-iso-date-format
    refitter ./openapi.json --additional-namespace "Your.Additional.Namespace" --additional-namespace "Your.Other.Additional.Namespace"
    refitter ./openapi.json --multiple-interfaces ByEndpoint
    refitter ./openapi.json --tag Pet --tag Store --tag User
    refitter ./openapi.json --match-path '^/pet/.*'
    refitter ./openapi.json --trim-unused-schema
    refitter ./openapi.json --trim-unused-schema  --keep-schema '^Model$' --keep-schema '^Person.+'
    refitter ./openapi.json --no-deprecated-operations
    refitter ./openapi.json --operation-name-template '{operationName}Async'
    refitter ./openapi.json --optional-nullable-parameters
    refitter ./openapi.json --use-apizr
    refitter ./openapi.json --use-dynamic-querystring-parameters

ARGUMENTS:
    [URL or input file]    URL or file path to OpenAPI Specification file

OPTIONS:
                                                    DEFAULT                                                                                                                                                    
    -h, --help                                                      Prints help information                                                                                                                   
    -v, --version                                                   Prints version information                                                                                                                
    -s, --settings-file                                             Path to .refitter settings file. Specifying this will ignore all other settings (except for --output)                                     
    -n, --namespace                                 GeneratedCode   Default namespace to use for generated types                                                                                              
    -o, --output                                    Output.cs       Path to Output file                                                                                                                       
        --no-auto-generated-header                                  Don't add <auto-generated> header to output file                                                                                          
        --no-accept-headers                                         Don't add <Accept> header to output file                                                                                                  
        --interface-only                                            Don't generate contract types                                                                                                             
        --use-api-response                                          Return Task<IApiResponse<T>> instead of Task<T>                                                                                           
        --use-observable-response                                   Return IObservable instead of Task                                                                                                        
        --internal                                                  Set the accessibility of the generated types to 'internal'                                                                                
        --cancellation-tokens                                       Use cancellation tokens                                                                                                                   
        --no-operation-headers                                      Don't generate operation headers                                                                                                          
        --no-logging                                                Don't log errors or collect telemetry                                                                                                     
        --additional-namespace                                      Add additional namespace to generated types                                                                                               
        --exclude-namespace                                         Exclude namespace on generated types                                                                                                      
        --use-iso-date-format                                       Explicitly format date query string parameters in ISO 8601 standard date format using delimiters (2023-06-15)                             
        --multiple-interfaces                                       Generate a Refit interface for each endpoint. May be one of ByEndpoint, ByTag                                                             
        --match-path                                                Only include Paths that match the provided regular expression. May be set multiple times                                                  
        --tag                                                       Only include Endpoints that contain this tag. May be set multiple times and result in OR'ed evaluation                                    
        --skip-validation                                           Skip validation of the OpenAPI specification                                                                                              
        --no-deprecated-operations                                  Don't generate deprecated operations                                                                                                      
        --operation-name-template                                   Generate operation names using pattern. When using --multiple-interfaces ByEndpoint, this is name of the Execute() method in the interface
        --optional-nullable-parameters                              Generate nullable parameters as optional parameters                                                                                       
        --trim-unused-schema                                        Removes unreferenced components schema to keep the generated output to a minimum                                                          
        --keep-schema                                               Force to keep matching schema, uses regular expressions. Use together with "--trim-unused-schema". Can be set multiple times              
        --no-banner                                                 Don't show donation banner                                                                                                                
        --skip-default-additional-properties                        Set to true to skip default additional properties                                                                                         
        --operation-name-generator                  Default         The NSwag IOperationNameGenerator implementation to use.                                                                                  
                                                                    May be one of:                                                                                                                            
                                                                    - Default                                                                                                                                 
                                                                    - MultipleClientsFromOperationId                                                                                                          
                                                                    - MultipleClientsFromPathSegments                                                                                                         
                                                                    - MultipleClientsFromFirstTagAndOperationId                                                                                               
                                                                    - MultipleClientsFromFirstTagAndOperationName                                                                                             
                                                                    - MultipleClientsFromFirstTagAndPathSegments                                                                                              
                                                                    - SingleClientFromOperationId                                                                                                             
                                                                    - SingleClientFromPathSegments                                                                                                            
                                                                    See https://refitter.github.io/api/Refitter.Core.OperationNameGeneratorTypes.html for more information                                    
        --immutable-records                                         Generate contracts as immutable records instead of classes 
        --use-apizr                                                 Set to true to use Apizr by:
                                                                    - Adding a final IApizrRequestOptions options parameter to all generated methods
                                                                    - Providing cancellation tokens by Apizr request options instead of a dedicated parameter
                                                                    - Using method overloads instead of optional parameters
                                                                    See https://refitter.github.io for more information and https://www.apizr.net to get started with Apizr                                                                               
        --use-dynamic-querystring-parameters                        Wrap multiple query parameters into a single complex one.
                                                                    See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information.
```

To generate code from an OpenAPI specifications file, run the following:

```shell
$ refitter [path to OpenAPI spec file] --namespace "[Your.Namespace.Of.Choice.GeneratedCode]"
```

This will generate a file called `Output.cs` which contains the Refit interface and contract classes generated using [NSwag](https://github.com/RicoSuter/NSwag)

## Source Generator

Refitter is available as a C# Source Generator that uses the [Refitter.Core](https://github.com/christianhelle/refitter/tree/main/src/Refitter.Core) library for generating a REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface from OpenAPI specifications. Refitter could format the generated Refit interface to be managed by [Apizr](https://www.apizr.net) and generate some registration helpers too.

The Refitter source generator is a bit untraditional in a sense that it creates a folder called `Generated` in the same location as the `.refitter` file and generates files to disk under the `Generated` folder (can be changed with `--outputFolder`). The source generator output should be included in the project and committed to source control. This is done because there is no other way to trigger the Refit source generator to pickup the Refitter generated code 

***(Translation: I couldn't for the life of me figure how to get that to work, sorry)***

### Installation

The source generator is distributed as a NuGet package and should be installed to the project that will contain the generated code

```shell
dotnet add package Refitter.SourceGenerator
```

### Usage

This source generator generates code based on any `.refitter` file included to the project as `AdditionalFiles`.

The generator can automatically detect all `.refitter` files inside the project that referenced the `Refitter.SourceGenerator` package and there is no need to include them manually as `AdditionalFiles`

### .Refitter File format

The following is an example `.refitter` file

```js
{
  "openApiPath": "/path/to/your/openAPI", // Required
  "namespace": "Org.System.Service.Api.GeneratedCode", // Optional. Default=GeneratedCode
  "naming": {
    "useOpenApiTitle": false, // Optional. Default=true
    "interfaceName": "MyApiClient" // Optional. Default=ApiClient
  },
  "generateContracts": true, // Optional. Default=true
  "generateXmlDocCodeComments": true, // Optional. Default=true
  "generateStatusCodeComments": true, // Optional. Default=true
  "addAutoGeneratedHeader": true, // Optional. Default=true
  "addAcceptHeaders": true, // Optional. Default=true
  "returnIApiResponse": false, // Optional. Default=false
  "responseTypeOverride": { // Optional. Default={}
    "File_Upload": "IApiResponse",
    "File_Download": "System.Net.Http.HttpContent"
  },
  "generateOperationHeaders": true, // Optional. Default=true
  "typeAccessibility": "Public", // Optional. Values=Public|Internal. Default=Public
  "useCancellationTokens": false, // Optional. Default=false
  "useIsoDateFormat": false, // Optional. Default=false
  "multipleInterfaces": "ByEndpoint", // Optional. May be one of "ByEndpoint" or "ByTag"
  "generateDeprecatedOperations": false, // Optional. Default=true
  "operationNameTemplate": "{operationName}Async", // Optional. Must contain {operationName} when multipleInterfaces != ByEndpoint
  "optionalParameters": false, // Optional. Default=false
  "outputFolder": "../CustomOutput" // Optional. Default=./Generated
  "outputFilename": "RefitInterface.cs", // Optional. Default=Output.cs for CLI tool
  "additionalNamespaces": [ // Optional
    "Namespace1",
    "Namespace2"
  ],
  "includeTags": [ // Optional. OpenAPI Tag to include when generating code
    "Pet",
    "Store",
    "User"
  ],
  "includePathMatches": [ // Optional. Only include Paths that match the provided regular expression
    "^/pet/.*",
    "^/store/.*"
  ],
  "trimUnusedSchema": false, // Optional. Default=false
  "keepSchemaPatterns": [ // Optional. Force to keep matching schema, uses regular expressions. Use together with trimUnusedSchema=true
    "^Model$",
    "^Person.+"
  ],
  "generateDefaultAdditionalProperties": true, // Optional. default=true
  "operationNameGenerator": "Default", // Optional. May be one of Default, MultipleClientsFromOperationId, MultipleClientsFromPathSegments, MultipleClientsFromFirstTagAndOperationId, MultipleClientsFromFirstTagAndOperationName, MultipleClientsFromFirstTagAndPathSegments, SingleClientFromOperationId, SingleClientFromPathSegments
  "immutableRecords": false,
  "useDynamicQuerystringParameters": true, // Optional. Default=false
  "dependencyInjectionSettings": { // Optional
    "baseUrl": "https://petstore3.swagger.io/api/v3", // Optional. Leave this blank to set the base address manually
    "httpMessageHandlers": [ // Optional
        "AuthorizationMessageHandler", 
        "TelemetryMessageHandler" 
    ],
    "usePolly": true, // DEPRECATED - Use "transientErrorHandler": "None|Polly|HttpResilience" instead
    "transientErrorHandler": "HttpResilience", // Optional. Set this to configure transient error handling with a retry policy that uses a jittered backoff. May be one of None, Polly, HttpResilience
    "maxRetryCount": 3, // Optional. Default=6
    "firstBackoffRetryInSeconds": 0.5 // Optional. Default=1.0
  },
  "apizrSettings": { // Optional
    "withRequestOptions": true, // Optional. Default=true
    "withRegistrationHelper": true, // Optional. Default=false
    "withCacheProvider": "InMemory", // Optional. Values=None|Akavache|MonkeyCache|InMemory|DistributedAsString|DistributedAsByteArray. Default=None
    "withPriority": true, // Optional. Default=false
    "withMediation": true, // Optional. Default=false
    "withOptionalMediation": true, // Optional. Default=false
    "withMappingProvider": "AutoMapper", // Optional. Values=None|AutoMapper|Mapster. Default=None
    "withFileTransfer": true // Optional. Default=false
  },
  "codeGeneratorSettings": { // Optional. Default settings are the values set in this example
    "requiredPropertiesMustBeDefined": true,
    "generateDataAnnotations": true,
    "anyType": "object",
    "dateType": "System.DateTimeOffset",
    "dateTimeType": "System.DateTimeOffset",
    "timeType": "System.TimeSpan",
    "timeSpanType": "System.TimeSpan",
    "arrayType": "System.Collections.Generic.ICollection",
    "dictionaryType": "System.Collections.Generic.IDictionary",
    "arrayInstanceType": "System.Collections.ObjectModel.Collection",
    "dictionaryInstanceType": "System.Collections.Generic.Dictionary",
    "arrayBaseType": "System.Collections.ObjectModel.Collection",
    "dictionaryBaseType": "System.Collections.Generic.Dictionary",
    "propertySetterAccessModifier": "",
    "generateImmutableArrayProperties": false,
    "generateImmutableDictionaryProperties": false,
    "handleReferences": false,
    "jsonSerializerSettingsTransformationMethod": null,
    "generateJsonMethods": false,
    "enforceFlagEnums": false,
    "inlineNamedDictionaries": false,
    "inlineNamedTuples": true,
    "inlineNamedArrays": false,
    "generateOptionalPropertiesAsNullable": false,
    "generateNullableReferenceTypes": false,
    "generateNativeRecords": false,
    "generateDefaultValues": true,
    "inlineNamedAny": false,
    "excludedTypeNames": [
      "ExcludedTypeFoo",
      "ExcludedTypeBar"
    ]
  }
}
```

- `openApiPath` - points to the OpenAPI Specifications file. This can be the path to a file stored on disk, relative to the `.refitter` file. This can also be a URL to a remote file that will be downloaded over HTTP/HTTPS
- `namespace` - the namespace used in the generated code. If not specified, this defaults to `GeneratedCode`
- `naming.useOpenApiTitle` - a boolean indicating whether the OpenApi title should be used. Default is `true`
- `naming.interfaceName` - the name of the generated interface. The generated code will automatically prefix this with `I` so if this set to `MyApiClient` then the generated interface is called `IMyApiClient`. Default is `ApiClient`
- `generateContracts` - a boolean indicating whether contracts should be generated. A use case for this is several API clients use the same contracts. Default is `true`
- `generateXmlDocCodeComments` - a boolean indicating whether XML doc comments should be generated. Default is `true`
- `generateStatusCodeComments` - a boolean indicating whether the XML docs for `ApiException` and `IApiResponse` contain detailed descriptions for every documented status code. Default is `true`
- `addAutoGeneratedHeader` - a boolean indicating whether XML doc comments should be generated. Default is `true`
- `addAcceptHeaders` -  a boolean indicating whether to add accept headers [Headers("Accept: application/json")]. Default is `true`
- `returnIApiResponse` - a boolean indicating whether to return `IApiResponse<T>` objects. Default is `false`
- `responseTypeOverride` - a dictionary with operation ids (as specified in the OpenAPI document) and a particular return type to use. The types are wrapped in a task, but otherwise unmodified (so make sure to specify or import their namespaces). Default is `{}`
- `generateOperationHeaders` - a boolean indicating whether to use operation headers in the generated methods. Default is `true`
- `typeAccessibility` - the generated type accessibility. Possible values are `Public` and `Internal`. Default is `Public`
- `useCancellationTokens` - Use cancellation tokens in the generated methods. Default is `false`
- `useIsoDateFormat` - Set to `true` to explicitly format date query string parameters in ISO 8601 standard date format using delimiters (for example: 2023-06-15). Default is `false`
- `multipleInterfaces` - Set to `ByEndpoint` to generate an interface for each endpoint, or `ByTag` to group Endpoints by their Tag (like SwaggerUI groups them).
- `outputFolder` - a string describing a relative path to a desired output folder. Default is `./Generated`
- `outputFilename` - Output filename. Default is `Output.cs` when used from the CLI tool, otherwise its the .refitter filename. So `Petstore.refitter` becomes `Petstore.cs`.
- `additionalNamespaces` - A collection of additional namespaces to include in the generated file. A use case for this is when you want to reuse contracts from a different namespace than the generated code. Default is empty
- `includeTags` - A collection of tags to use a filter for including endpoints that contain this tag.
- `includePathMatches` - A collection of regular expressions used to filter paths.
- `generateDeprecatedOperations` - a boolean indicating whether deprecated operations should be generated or skipped. Default is `true`
- `operationNameTemplate` - Generate operation names using pattern. This must contain the string {operationName}. An example usage of this could be `{operationName}Async` to suffix all method names with Async
- `optionalParameters` - Generate non-required parameters as nullable optional parameters
- `trimUnusedSchema` - Removes unreferenced components schema to keep the generated output to a minimum
- `keepSchemaPatterns`: A collection of regular expressions to force to keep matching schema. This is used together with `trimUnusedSchema`
- `generateDefaultAdditionalProperties`: Set to `false` to skip default additional properties. Default is `true`
- `operationNameGenerator`: The NSwag `IOperationNameGenerator` implementation to use. See https://refitter.github.io/api/Refitter.Core.OperationNameGeneratorTypes.html
- `immutableRecords`: Set to `true` to generate contracts as immutable records instead of classes. Default is `false`
- `useDynamicQuerystringParameters`: Set to `true` to wrap multiple query parameters into a single complex one. Default is `false` (no wrapping). See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information.
- `dependencyInjectionSettings` - Setting this will generated extension methods to `IServiceCollection` for configuring Refit clients
  - `baseUrl` - Used as the HttpClient base address. Leave this blank to manually set the base URL
  - `httpMessageHandlers` - A collection of `HttpMessageHandler` that is added to the HttpClient pipeline
  - `usePolly` - (DEPRECATED) Set this to true to configure the HttpClient to use Polly using a retry policy with a jittered backoff
  - `transientErrorHandler`: Set this to configure transient error handling with a retry policy that uses a jittered backoff. See https://refitter.github.io/api/Refitter.Core.TransientErrorHandler.html
  - `firstBackoffRetryInSeconds` - This is the duration of the initial retry backoff. Default is 1 second
- `apizrSettings` - Setting this will format Refit interface to be managed by Apizr. See https://www.apizr.net for more information
  - `withRequestOptions` - Tells if the Refit interface methods should have a final IApizrRequestOptions options parameter
  - `withRegistrationHelper` - Tells if Refitter should generate Apizr registration helpers (extended with dependencyInjectionSettings set, otherwise static)
  - `withCacheProvider` - Set the cache provider to be used
  - `withPriority` - Tells if Apizr should handle request priority
  - `withMediation` - Tells if Apizr should handle request mediation (extended only)
  - `withOptionalMediation` - Tells if Apizr should handle optional request mediation (extended only)
  - `withMappingProvider` - Set the mapping provider to be used
  - `withFileTransfer` - Tells if Apizr should handle file transfer
- `codeGeneratorSettings` - Setting this allows customization of the NSwag generated types and contracts
  - `requiredPropertiesMustBeDefined` - Default is true,
  - `generateDataAnnotations` - Default is true,
  - `anyType` - Default is `object`,
  - `dateType` - Default is `System.DateTimeOffset`,
  - `dateTimeType` - Default is `System.DateTimeOffset`,
  - `timeType` - Default is `System.TimeSpan`,
  - `timeSpanType` - Default is `System.TimeSpan`,
  - `arrayType` - Default is `System.Collections.Generic.ICollection`,
  - `dictionaryType` - Default is `System.Collections.Generic.IDictionary`,
  - `arrayInstanceType` - Default is `System.Collections.ObjectModel.Collection`,
  - `dictionaryInstanceType` - Default is `System.Collections.Generic.Dictionary`,
  - `arrayBaseType` - Default is `System.Collections.ObjectModel.Collection`,
  - `dictionaryBaseType` - Default is `System.Collections.Generic.Dictionary`,
  - `propertySetterAccessModifier` - Default is ``,
  - `generateImmutableArrayProperties` - Default is false,
  - `generateImmutableDictionaryProperties` - Default is false,
  - `handleReferences` - Default is false,
  - `jsonSerializerSettingsTransformationMethod` - Default is null,
  - `generateJsonMethods` - Default is false,
  - `enforceFlagEnums` - Default is false,
  - `inlineNamedDictionaries` - Default is false,
  - `inlineNamedTuples` - Default is true,
  - `inlineNamedArrays` - Default is false,
  - `generateOptionalPropertiesAsNullable` - Default is false,
  - `generateNullableReferenceTypes` - Default is false,
  - `generateNativeRecords` - Default is false
  - `generateDefaultValues` - Default is true
  - `inlineNamedAny` - Default is false
  - `excludedTypeNames` - Default is empty


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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Your.Namespace.Of.Choice.GeneratedCode
{
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ISwaggerPetstore
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
        /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions</remarks>
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
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Your.Namespace.Of.Choice.GeneratedCode
{
    [System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
    public partial interface ISwaggerPetstore
    {
        /// <summary>Update an existing pet</summary>
        /// <remarks>Update an existing pet by Id</remarks>
        /// <param name="body">Update an existent pet in the store</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term>200</term>
        /// <description>Successful operation</description>
        /// </item>
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
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Put("/pet")]
        Task<IApiResponse<Pet>> UpdatePet([Body] Pet body);

        /// <summary>Add a new pet to the store</summary>
        /// <remarks>Add a new pet to the store</remarks>
        /// <param name="body">Create a new pet in the store</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term>200</term>
        /// <description>Successful operation</description>
        /// </item>
        /// <item>
        /// <term>405</term>
        /// <description>Invalid input</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Post("/pet")]
        Task<IApiResponse<Pet>> AddPet([Body] Pet body);

        /// <summary>Finds Pets by status</summary>
        /// <remarks>Multiple status values can be provided with comma separated strings</remarks>
        /// <param name="status">Status values that need to be considered for filter</param>
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
        /// <item>
        /// <term>400</term>
        /// <description>Invalid status value</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/pet/findByStatus")]
        Task<IApiResponse<ICollection<Pet>>> FindPetsByStatus([Query] Status? status);

        /// <summary>Finds Pets by tags</summary>
        /// <remarks>Multiple tags can be provided with comma separated strings. Use tag1, tag2, tag3 for testing.</remarks>
        /// <param name="tags">Tags to filter by</param>
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
        /// <item>
        /// <term>400</term>
        /// <description>Invalid tag value</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/pet/findByTags")]
        Task<IApiResponse<ICollection<Pet>>> FindPetsByTags([Query(CollectionFormat.Multi)] IEnumerable<string> tags);

        /// <summary>Find pet by ID</summary>
        /// <remarks>Returns a single pet</remarks>
        /// <param name="petId">ID of pet to return</param>
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
        /// <item>
        /// <term>400</term>
        /// <description>Invalid ID supplied</description>
        /// </item>
        /// <item>
        /// <term>404</term>
        /// <description>Pet not found</description>
        /// </item>
        /// </list>
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
        /// </returns>
        [Post("/pet/{petId}")]
        Task<IApiResponse> UpdatePetWithForm(long petId, [Query] string name, [Query] string status);

        /// <summary>Deletes a pet</summary>
        /// <param name="petId">Pet id to delete</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
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
        /// </returns>
        [Delete("/pet/{petId}")]
        Task<IApiResponse> DeletePet(long petId, [Header("api_key")] string api_key);

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
        Task<IApiResponse<ApiResponse>> UploadFile(long petId, [Query] string additionalMetadata,  StreamPart body);

        /// <summary>Returns pet inventories by status</summary>
        /// <remarks>Returns a map of status codes to quantities</remarks>
        /// <returns>successful operation</returns>
        /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
        [Headers("Accept: application/json")]
        [Get("/store/inventory")]
        Task<IApiResponse<IDictionary<string, int>>> GetInventory();

        /// <summary>Place an order for a pet</summary>
        /// <remarks>Place a new order in the store</remarks>
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
        /// <item>
        /// <term>405</term>
        /// <description>Invalid input</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/json")]
        [Post("/store/order")]
        Task<IApiResponse<Order>> PlaceOrder([Body] Order body);

        /// <summary>Find purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions</remarks>
        /// <param name="orderId">ID of order that needs to be fetched</param>
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
        /// <item>
        /// <term>400</term>
        /// <description>Invalid ID supplied</description>
        /// </item>
        /// <item>
        /// <term>404</term>
        /// <description>Order not found</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/json")]
        [Get("/store/order/{orderId}")]
        Task<IApiResponse<Order>> GetOrderById(long orderId);

        /// <summary>Delete purchase order by ID</summary>
        /// <remarks>For valid response try integer IDs with value < 1000. Anything above 1000 or nonintegers will generate API errors</remarks>
        /// <param name="orderId">ID of the order that needs to be deleted</param>
        /// <returns>
        /// A <see cref="Task"/> representing the <see cref="IApiResponse"/> instance containing the result:
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
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term>200</term>
        /// <description>Successful operation</description>
        /// </item>
        /// </list>
        /// </returns>
        [Headers("Accept: application/xml, application/json")]
        [Post("/user/createWithList")]
        Task<IApiResponse<User>> CreateUsersWithListInput([Body] IEnumerable<User> body);

        /// <summary>Logs user into the system</summary>
        /// <param name="username">The user name for login</param>
        /// <param name="password">The password for login in clear text</param>
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
        /// <item>
        /// <term>400</term>
        /// <description>Invalid username/password supplied</description>
        /// </item>
        /// </list>
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
        /// <list type="table">
        /// <listheader>
        /// <term>Status</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term>200</term>
        /// <description>successful operation</description>
        /// </item>
        /// <item>
        /// <term>400</term>
        /// <description>Invalid username supplied</description>
        /// </item>
        /// <item>
        /// <term>404</term>
        /// <description>User not found</description>
        /// </item>
        /// </list>
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
    [Headers("Accept: application/xml, application/json")]
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
    [Headers("Accept: application/xml, application/json")]
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
    [Headers("Accept: application/json")]
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
    [Headers("Accept: application/json")]
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
    [Headers("Accept: application/xml, application/json")]
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
    [Headers("Accept: application/json")]
    [Post("/pet/{petId}/uploadImage")]
    Task<ApiResponse> Execute(long petId, [Query] string additionalMetadata,  StreamPart body);
}

/// <summary>Returns pet inventories by status</summary>
[System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
public partial interface IGetInventoryEndpoint
{
    /// <summary>Returns pet inventories by status</summary>
    /// <remarks>Returns a map of status codes to quantities</remarks>
    /// <returns>successful operation</returns>
    /// <exception cref="ApiException">Thrown when the request returns a non-success status code.</exception>
    [Headers("Accept: application/json")]
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
    [Headers("Accept: application/json")]
    [Post("/store/order")]
    Task<Order> Execute([Body] Order body);
}

/// <summary>Find purchase order by ID</summary>
[System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
public partial interface IGetOrderByIdEndpoint
{
    /// <summary>Find purchase order by ID</summary>
    /// <remarks>For valid response try integer IDs with value <= 5 or > 10. Other values will generated exceptions</remarks>
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
    Task<Order> Execute(long orderId);
}

/// <summary>Delete purchase order by ID</summary>
[System.CodeDom.Compiler.GeneratedCode("Refitter", "1.0.0.0")]
public partial interface IDeleteOrderEndpoint
{
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
    [Headers("Accept: application/json, application/xml")]
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
    [Headers("Accept: application/xml, application/json")]
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
    [Headers("Accept: application/json")]
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
    [Headers("Accept: application/json")]
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
```

Here's an example generated output from the [Swagger Petstore example](https://petstore3.swagger.io) configured to generate an interface with dynamic querystring paremeters

**CLI Tool**

```bash
$ refitter ./openapi.json --namespace "Your.Namespace.Of.Choice.GeneratedCode" --use-dynamic-querystring-parameters
```

**Output**

```cs
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

## ASP.NET Core and HttpClientFactory

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
- Using method overloads instead of optional parameters (note that setting `useDynamicQuerystringParameters` to `true` improve overloading experience)

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

## System requirements
.NET 8.0

## Contributors

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/neoGeneva"><img src="https://avatars.githubusercontent.com/u/804724?v=4?s=100" width="100px;" alt="Philip Cox"/><br /><sub><b>Philip Cox</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=neoGeneva" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://cam.macfar.land/"><img src="https://avatars.githubusercontent.com/u/1298847?v=4?s=100" width="100px;" alt="Cameron MacFarland"/><br /><sub><b>Cameron MacFarland</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=distantcam" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://kgame.tw"><img src="https://avatars.githubusercontent.com/u/3646532?v=4?s=100" width="100px;" alt="kgame"/><br /><sub><b>kgame</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=kgamecarter" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://yrki.no"><img src="https://avatars.githubusercontent.com/u/11573601?v=4?s=100" width="100px;" alt="Thomas Pettersen / Yrki"/><br /><sub><b>Thomas Pettersen / Yrki</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/1kvin"><img src="https://avatars.githubusercontent.com/u/46425980?v=4?s=100" width="100px;" alt="Artem"/><br /><sub><b>Artem</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3A1kvin" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/m7clarke"><img src="https://avatars.githubusercontent.com/u/47439144?v=4?s=100" width="100px;" alt="m7clarke"/><br /><sub><b>m7clarke</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Am7clarke" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/kirides"><img src="https://avatars.githubusercontent.com/u/13602143?v=4?s=100" width="100px;" alt="kirides"/><br /><sub><b>kirides</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Akirides" title="Bug reports">🐛</a> <a href="https://github.com/christianhelle/refitter/commits?author=kirides" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/guillaumeserale"><img src="https://avatars.githubusercontent.com/u/6672406?v=4?s=100" width="100px;" alt="guillaumeserale"/><br /><sub><b>guillaumeserale</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=guillaumeserale" title="Code">💻</a> <a href="https://github.com/christianhelle/refitter/issues?q=author%3Aguillaumeserale" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Roflincopter"><img src="https://avatars.githubusercontent.com/u/1690243?v=4?s=100" width="100px;" alt="Dennis Brentjes"/><br /><sub><b>Dennis Brentjes</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=Roflincopter" title="Code">💻</a> <a href="#ideas-Roflincopter" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.linkedin.com/in/hickeydamian/"><img src="https://avatars.githubusercontent.com/u/57436?v=4?s=100" width="100px;" alt="Damian Hickey"/><br /><sub><b>Damian Hickey</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Adamianh" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/richardhu-lmg"><img src="https://avatars.githubusercontent.com/u/126430787?v=4?s=100" width="100px;" alt="richardhu-lmg"/><br /><sub><b>richardhu-lmg</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Arichardhu-lmg" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/brease-colin"><img src="https://avatars.githubusercontent.com/u/47358935?v=4?s=100" width="100px;" alt="brease-colin"/><br /><sub><b>brease-colin</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Abrease-colin" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/angelofb"><img src="https://avatars.githubusercontent.com/u/2032257?v=4?s=100" width="100px;" alt="angelofb"/><br /><sub><b>angelofb</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=angelofb" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/NoGRo"><img src="https://avatars.githubusercontent.com/u/5665273?v=4?s=100" width="100px;" alt="Dim Nogro"/><br /><sub><b>Dim Nogro</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=NoGRo" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/yadanilov19"><img src="https://avatars.githubusercontent.com/u/17161065?v=4?s=100" width="100px;" alt="yadanilov19"/><br /><sub><b>yadanilov19</b></sub></a><br /><a href="#ideas-yadanilov19" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/commits?author=yadanilov19" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/danpowell88"><img src="https://avatars.githubusercontent.com/u/1100397?v=4?s=100" width="100px;" alt="Daniel Powell"/><br /><sub><b>Daniel Powell</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Adanpowell88" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Ekkeir"><img src="https://avatars.githubusercontent.com/u/36194685?v=4?s=100" width="100px;" alt="Ekkeir"/><br /><sub><b>Ekkeir</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=Ekkeir" title="Documentation">📖</a> <a href="https://github.com/christianhelle/refitter/issues?q=author%3AEkkeir" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/waylonmtz"><img src="https://avatars.githubusercontent.com/u/4604579?v=4?s=100" width="100px;" alt="Waylon Martinez"/><br /><sub><b>Waylon Martinez</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Awaylonmtz" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/vinaymadupathi"><img src="https://avatars.githubusercontent.com/u/38102900?v=4?s=100" width="100px;" alt="vkmadupa"/><br /><sub><b>vkmadupa</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Avinaymadupathi" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Noblix"><img src="https://avatars.githubusercontent.com/u/22076883?v=4?s=100" width="100px;" alt="Noblix"/><br /><sub><b>Noblix</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=Noblix" title="Code">💻</a> <a href="#ideas-Noblix" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.veezla.io"><img src="https://avatars.githubusercontent.com/u/230432?v=4?s=100" width="100px;" alt="Attila Hajdrik"/><br /><sub><b>Attila Hajdrik</b></sub></a><br /><a href="#ideas-attilah" title="Ideas, Planning, & Feedback">🤔</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/bielik01"><img src="https://avatars.githubusercontent.com/u/920950?v=4?s=100" width="100px;" alt="bielik01"/><br /><sub><b>bielik01</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Abielik01" title="Bug reports">🐛</a> <a href="#ideas-bielik01" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/naaeef"><img src="https://avatars.githubusercontent.com/u/43339071?v=4?s=100" width="100px;" alt="naaeef"/><br /><sub><b>naaeef</b></sub></a><br /><a href="#ideas-naaeef" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/alrz"><img src="https://avatars.githubusercontent.com/u/3105979?v=4?s=100" width="100px;" alt="Alireza Habibi"/><br /><sub><b>Alireza Habibi</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Aalrz" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/EEParker"><img src="https://avatars.githubusercontent.com/u/15874076?v=4?s=100" width="100px;" alt="Jeff Parker, PE"/><br /><sub><b>Jeff Parker, PE</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3AEEParker" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jods4"><img src="https://avatars.githubusercontent.com/u/3832820?v=4?s=100" width="100px;" alt="jods"/><br /><sub><b>jods</b></sub></a><br /><a href="#ideas-jods4" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/issues?q=author%3Ajods4" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/edimarquez"><img src="https://avatars.githubusercontent.com/u/41791719?v=4?s=100" width="100px;" alt="Edimarquez Medeiros"/><br /><sub><b>Edimarquez Medeiros</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=edimarquez" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/safakkesikci"><img src="https://avatars.githubusercontent.com/u/7067252?v=4?s=100" width="100px;" alt="safakkesikci"/><br /><sub><b>safakkesikci</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Asafakkesikci" title="Bug reports">🐛</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/folbrecht"><img src="https://avatars.githubusercontent.com/u/145537846?v=4?s=100" width="100px;" alt="folbrecht"/><br /><sub><b>folbrecht</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Afolbrecht" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/mortenlaursen"><img src="https://avatars.githubusercontent.com/u/28759737?v=4?s=100" width="100px;" alt="mortenlaursen"/><br /><sub><b>mortenlaursen</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=mortenlaursen" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/manuel-fernandez-rodriguez"><img src="https://avatars.githubusercontent.com/u/48520205?v=4?s=100" width="100px;" alt="manuel-fernandez-rodriguez"/><br /><sub><b>manuel-fernandez-rodriguez</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Amanuel-fernandez-rodriguez" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/eliyammine"><img src="https://avatars.githubusercontent.com/u/6644807?v=4?s=100" width="100px;" alt="Eli Yammine"/><br /><sub><b>Eli Yammine</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Aeliyammine" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/kami-poi"><img src="https://avatars.githubusercontent.com/u/47322691?v=4?s=100" width="100px;" alt="kami-poi"/><br /><sub><b>kami-poi</b></sub></a><br /><a href="#ideas-kami-poi" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Xeevis"><img src="https://avatars.githubusercontent.com/u/5835044?v=4?s=100" width="100px;" alt="Xeevis"/><br /><sub><b>Xeevis</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3AXeevis" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/DJ4ddi"><img src="https://avatars.githubusercontent.com/u/1696102?v=4?s=100" width="100px;" alt="DJ4ddi"/><br /><sub><b>DJ4ddi</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=DJ4ddi" title="Code">💻</a> <a href="#ideas-DJ4ddi" title="Ideas, Planning, & Feedback">🤔</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/direncancatalkaya"><img src="https://avatars.githubusercontent.com/u/57223732?v=4?s=100" width="100px;" alt="direncancatalkaya"/><br /><sub><b>direncancatalkaya</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=direncancatalkaya" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/robpalm"><img src="https://avatars.githubusercontent.com/u/14939530?v=4?s=100" width="100px;" alt="Robert Palmqvist"/><br /><sub><b>Robert Palmqvist</b></sub></a><br /><a href="#ideas-robpalm" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/commits?author=robpalm" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/TimothyMakkison"><img src="https://avatars.githubusercontent.com/u/49349513?v=4?s=100" width="100px;" alt="Tim M"/><br /><sub><b>Tim M</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=TimothyMakkison" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/janfolbrecht"><img src="https://avatars.githubusercontent.com/u/42186604?v=4?s=100" width="100px;" alt="janfolbrecht"/><br /><sub><b>janfolbrecht</b></sub></a><br /><a href="#ideas-janfolbrecht" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/commits?author=janfolbrecht" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/osc-nseguin"><img src="https://avatars.githubusercontent.com/u/133910309?v=4?s=100" width="100px;" alt="Nick Seguin"/><br /><sub><b>Nick Seguin</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/commits?author=osc-nseguin" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/david-brink-talogy"><img src="https://avatars.githubusercontent.com/u/43828739?v=4?s=100" width="100px;" alt="David Brink"/><br /><sub><b>David Brink</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Adavid-brink-talogy" title="Bug reports">🐛</a> <a href="https://github.com/christianhelle/refitter/commits?author=david-brink-talogy" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/dammitjanet"><img src="https://avatars.githubusercontent.com/u/952723?v=4?s=100" width="100px;" alt="Stu Wilson"/><br /><sub><b>Stu Wilson</b></sub></a><br /><a href="#ideas-dammitjanet" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/commits?author=dammitjanet" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/sharpzilla"><img src="https://avatars.githubusercontent.com/u/16763634?v=4?s=100" width="100px;" alt="sharpzilla"/><br /><sub><b>sharpzilla</b></sub></a><br /><a href="#ideas-sharpzilla" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Jappinen"><img src="https://avatars.githubusercontent.com/u/44408497?v=4?s=100" width="100px;" alt="Tatu"/><br /><sub><b>Tatu</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3AJappinen" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://www.respawnsive.com"><img src="https://avatars.githubusercontent.com/u/4272307?v=4?s=100" width="100px;" alt="Jérémy BRUN-PICARD"/><br /><sub><b>Jérémy BRUN-PICARD</b></sub></a><br /><a href="#ideas-JeremyBP" title="Ideas, Planning, & Feedback">🤔</a> <a href="https://github.com/christianhelle/refitter/commits?author=JeremyBP" title="Code">💻</a> <a href="https://github.com/christianhelle/refitter/commits?author=JeremyBP" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ebarnard"><img src="https://avatars.githubusercontent.com/u/1059683?v=4?s=100" width="100px;" alt="Ed Barnard"/><br /><sub><b>Ed Barnard</b></sub></a><br /><a href="#ideas-ebarnard" title="Ideas, Planning, & Feedback">🤔</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/bastiennoel93"><img src="https://avatars.githubusercontent.com/u/18250350?v=4?s=100" width="100px;" alt="bastien.noel"/><br /><sub><b>bastien.noel</b></sub></a><br /><a href="https://github.com/christianhelle/refitter/issues?q=author%3Abastiennoel93" title="Bug reports">🐛</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/MeikelLP"><img src="https://avatars.githubusercontent.com/u/11669846?v=4?s=100" width="100px;" alt="Meikel Philipp"/><br /><sub><b>Meikel Philipp</b></sub></a><br /><a href="#ideas-MeikelLP" title="Ideas, Planning, & Feedback">🤔</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

#

For tips and tricks on software development, check out [my blog](https://christianhelle.com)

If you find this useful and feel a bit generous then feel free to [buy me a coffee ☕](https://www.buymeacoffee.com/christianhelle)
