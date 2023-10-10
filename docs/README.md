# Documentation

The documentation for Refitter is published to https://refitter.github.io. This includes articles on getting started and general usage of the tool. The core library, Refitter.Core is also documented at the API level, for those who wish to incorporate Refitter into their own projects

## Running the documentation page locally

The docs website is build using [docfx](https://dotnet.github.io/docfx/) and is published using Github Pages hosted on the [Refitter Github organization](https://github.com/refitter)

To run docs website locally you need to install the latest version of `docfx`

```bash
dotnet tool update -g docfx
```

and from the `docs` folder run `docfx` with the `--serve` argument

```bash
docfx docfx_project/docfx.json --serve
```