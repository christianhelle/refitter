[![Build](https://github.com/christianhelle/refitter/actions/workflows/build.yml/badge.svg)](https://github.com/christianhelle/refitter/actions/workflows/build.yml)

# Refitter
Refitter is a CLI tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. 

## System requirements
.NET 7.0

### Installation
```
$ dotnet tool install --global refitter
```

## Usage:

To generate code from an OpenAPI specifications file, run the following:

```
$ refitter [path to OpenAPI spec file]
```

This will generate a file called `Output.cs` which contains the Refit interface and contract classes generated using [NSwag](https://github.com/RicoSuter/NSwag)

