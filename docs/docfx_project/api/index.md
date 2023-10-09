## Using Refitter.Core

To use the [RefitGenerator](Refitter.Core.RefitGenerator.yml) class, you need to follow these steps:

1. Install the [Refitter.Core](Refitter.Core.yml) NuGet package in your project.
2. Create an instance of the [RefitGeneratorSettings](Refitter.Core.RefitGeneratorSettings.yml) class, which contains the settings used to configure the generator. You need to set the `OpenApiPath` property to the path of your OpenAPI specification file or a URL.
3. Create an instance of the [RefitGenerator](Refitter.Core.RefitGenerator.yml) class by calling the `CreateAsync` method and passing in the [RefitGeneratorSettings](Refitter.Core.RefitGeneratorSettings.yml) instance.
4. Call the `Generate())` method on the `RefitGenerator` instance to generate the Refit clients and interfaces based on the OpenAPI specification. This method returns the generated code as a string.

Here's an example of how to use the [RefitGenerator](Refitter.Core.RefitGenerator.yml) class:

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

