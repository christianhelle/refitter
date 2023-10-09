To use the [RefitGenerator](Refitter.Core.RefitGenerator.yml) class, you need to follow these steps:

1. Install the [Refitter.Core](Refitter.Core.yml) NuGet package in your project.
2. Create an instance of the [RefitGeneratorSettings](Refitter.Core.RefitGeneratorSettings.yml) class, which contains the settings used to configure the generator. You need to set the [OpenApiPath](Refitter.Core.RefitGeneratorSettings.yml#Refitter_Core_RefitGeneratorSettings_OpenApiPath) property to the path of your OpenAPI specification file or a URL.
3. Create an instance of the [RefitGenerator](Refitter.Core.RefitGenerator.yml) class by calling the [CreateAsync()](Refitter.Core.RefitGenerator.yml#Refitter_Core_RefitGenerator_CreateAsync_Refitter_Core_RefitGeneratorSettings_) method and passing in the [RefitGeneratorSettings](Refitter.Core.RefitGeneratorSettings.yml) instance.
4. Call the [Generate()](Refitter.Core.RefitGenerator.yml#Refitter_Core_RefitGenerator_Generate) method on the [RefitGenerator](Refitter.Core.RefitGenerator.yml) instance to generate the Refit clients and interfaces based on the OpenAPI specification. This method returns the generated code as a string.

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

