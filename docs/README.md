# Documentation

The documentation for Refitter is published to [https://refitter.github.io](https://refitter.github.io). This includes articles on getting started and general usage of the tool. The core library, Refitter.Core is also documented at the API level, for those who wish to incorporate Refitter into their own projects.

## Running the Documentation Page Locally

The docs website is built using [DocFX](https://dotnet.github.io/docfx/) and is published using GitHub Pages hosted on the [Refitter GitHub organization](https://github.com/refitter).

To run the docs website locally, you need to install the latest version of `docfx`:

```bash
dotnet tool update -g docfx
```

From the `docs` folder, run `docfx` with the `--serve` argument:

```bash
docfx docfx_project/docfx.json --serve
```

The documentation site will be available at `http://localhost:8080`.

## Documentation Structure

- **Articles**: Located in `docfx_project/articles/`, contains guides and tutorials
- **API Documentation**: Auto-generated from XML documentation comments in the source code
- **Examples**: Code examples and usage scenarios

## Contributing to Documentation

When contributing to the documentation:

1. Follow the existing markdown style and formatting
2. Include code examples where appropriate
3. Test your changes locally before submitting
4. Ensure all links are valid and working
5. Update the table of contents if adding new articles