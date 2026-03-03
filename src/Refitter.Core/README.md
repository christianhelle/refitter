To use the `RefitGenerator` class, you need to follow these steps:

1. Install the `Refitter.Core` NuGet package in your project.
2. Create an instance of the `RefitGeneratorSettings` class, which contains the settings used to configure the generator. Set the `OpenApiPath` property to the path of your OpenAPI specification file or a URL. Alternatively, set the `OpenApiPaths` property to an array of paths when you want to merge multiple OpenAPI specifications into a single client.
3. Create an instance of the `RefitGenerator` class by calling the `CreateAsync` method and passing in the `RefitGeneratorSettings` instance.
4. Call the `Generate()` method on the `RefitGenerator` instance to generate the Refit clients and interfaces based on the OpenAPI specification. This method returns the generated code as a string.

Here's an example of how to use the `RefitGenerator` class with a single OpenAPI specification:

```csharp
using Refit;
using Refitter.Core;

// Create an instance of the RefitGeneratorSettings class
var settings = new RefitGeneratorSettings
{
    OpenApiPath = "Relative or absolute path to .json or .yaml local file or a URL to a .json or .yaml file"
};

// Create an instance of the RefitGenerator class
var generator = await RefitGenerator.CreateAsync(settings);

// Generate the Refit clients and interfaces and get the generated code as a string
var generatedCode = await generator.Generate();

// Use the generated code in your project
```

Here's an example of how to merge multiple OpenAPI specifications into a single client:

```csharp
using Refit;
using Refitter.Core;

// Create an instance of the RefitGeneratorSettings class with multiple OpenAPI specs
var settings = new RefitGeneratorSettings
{
    OpenApiPaths = new[]
    {
        "path/to/openapi-v1.json",
        "path/to/openapi-v2.json"
    }
};

// Create an instance of the RefitGenerator class
var generator = await RefitGenerator.CreateAsync(settings);

// Generate the merged Refit clients and interfaces
var generatedCode = await generator.Generate();

// Use the generated code in your project
```

