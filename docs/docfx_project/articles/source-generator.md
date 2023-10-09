# Source Generator

Refitter is available as a C# Source Generator that uses the [Refitter.Core](/api/Refitter.Core.html) library for generating a REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface from OpenAPI specifications

The Refitter source generator is a bit untraditional in a sense that it creates a folder called `Generated` in the same location as the `.refitter` file and generates files to disk under the `Generated` folder (can be changed with `--outputFolder`). The source generator output should be included in the project and committed to source control. This is done because there is no other way to trigger the Refit source generator to pickup the Refitter generated code 

***(Translation: I couldn't for the life of me figure how to get that to work, sorry)***

### Installation

The source generator is distributed as a NuGet package and should be installed to the project that will contain the generated code

```shell
dotnet add package Refitter.SourceGenerator
```

### Usage

This source generator generates code based on any [.refitter](refitter-file-format.md) file included to the project as `AdditionalFiles`.

The generator can automatically detect all [.refitter](refitter-file-format.md) files inside the project that referenced the `Refitter.SourceGenerator` package and there is no need to include them manually as `AdditionalFiles`