# Refitter
Refitter is a tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface and contracts from OpenAPI specifications. 

Refitter comes in 2 forms:
- A [.NET CLI Tool](#cli-tool) distributed via [nuget.org](http://www.nuget.org/packages/refitter) that outputs a single C# file on disk
- A [C# Source Generator](#source-generator) via the [Refitter.SourceGenerator](http://www.nuget.org/packages/refitter.sourcegenerator) package that generates code on compile time based on a [.refitter](#.refitter-file-format) within the project directory.
