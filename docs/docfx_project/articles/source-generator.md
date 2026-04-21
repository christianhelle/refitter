## Source Generator

Refitter is available as a C# Source Generator that uses the [Refitter.Core](/api/Refitter.Core.html) library for generating a REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface from OpenAPI specifications. Refitter could format the generated Refit interface to be managed by [Apizr](https://www.apizr.net) (v6+) and generate some registration helpers too.

Starting with v2.0.0, the source generator emits code in-memory through Roslyn `AddSource()` instead of writing `.g.cs` files to disk. Use your IDE's generated-files view to inspect the output. If your workflow requires physical generated files for review, commits, or build scripts, use Refitter CLI or Refitter.MSBuild instead.

#### Installation

The source generator is distributed as a NuGet package and should be installed to the project that will contain the generated code

```shell
dotnet add package Refitter.SourceGenerator
dotnet add package Refit
```

`Refitter.SourceGenerator` keeps its `Refit` dependency private, so consuming projects must add their own direct package reference to `Refit`. If you use generated dependency-injection helpers such as `ConfigureRefitClients()`, also add `Refit.HttpClientFactory` explicitly.

#### Usage

This source generator generates code based on any [.refitter](refitter-file-format.md) file included to the project as `AdditionalFiles`.

The generator can automatically detect all [.refitter](refitter-file-format.md) files inside the project that referenced the `Refitter.SourceGenerator` package and there is no need to include them manually as `AdditionalFiles`
