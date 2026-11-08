## Source Generator

Refitter is available as a C# Source Generator that uses the [Refitter.Core](/api/Refitter.Core.html) library for generating a REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface from OpenAPI specifications. Refitter could format the generated Refit interface to be managed by [Apizr](https://www.apizr.net) (v6+) and generate some registration helpers too.

The source generator emits code in-memory through Roslyn `AddSource()` and also generates the Refit implementations for it, so a single build is enough. Use your IDE's generated-files view to inspect the output. If your workflow requires physical generated files for review, commits, or build scripts, use Refitter CLI or Refitter.MSBuild instead.

#### Installation

The source generator is distributed as a NuGet package and should be installed to the project that will contain the generated code

```shell
dotnet add package Refitter.SourceGenerator
dotnet add package Refit
```

`Refitter.SourceGenerator` keeps its `Refit` dependency private, so consuming projects must add their own direct package reference to `Refit`. If you use generated dependency-injection helpers such as `ConfigureRefitClients()`, also add `Refit.HttpClientFactory` explicitly.

#### Usage

After installing the package, add one or more [.refitter](refitter-file-format.md) files to your project.

`Refitter.SourceGenerator` automatically includes `**/*.refitter` as Roslyn `AdditionalFiles` via its package props, so you do not need to add manual `<AdditionalFiles Include="..." />` entries unless you intentionally override that default behavior.

The generated code is added to the compilation in memory, together with the Refit implementations, in a single build. Nothing is written to disk and nothing needs to be committed to source control. The `outputFolder`, `outputFilename`, and `contractsOutputFolder` settings don't create files when using the source generator. To inspect the generated code on disk, set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in the project file.

#### How it works with the Refit source generator

Roslyn runs every source generator against the same input, so the Refit source generator can't see the interfaces that Refitter generates. To produce everything in one build, `Refitter.SourceGenerator` sets `DisableRefitSourceGenerator` to `true` for the regular compiler pass and runs the Refit source generator from the project's own `Refit` package itself, over the project code plus the Refitter output. Hand-written Refit interfaces in the same project are generated as before. Refit analyzer checks that only run while the Refit source generator is enabled, such as `RF006` (and `RefitRequireGeneratedRequests`), are still reported.

For the same reason, when `generateJsonSerializerContext` is enabled, Refitter also runs the System.Text.Json source generator over the `JsonSerializerContext` it generates.

If the project sets `DisableRefitSourceGenerator` to `true` itself, for example when using `Refit.Reflection`, Refitter leaves Refit source generation off and only emits its own code.

#### Telemetry

`Refitter.SourceGenerator` collects usage telemetry, the same as the Refitter CLI and `Refitter.MSBuild`. It reports which settings each `.refitter` file uses and the number of `.refitter` files in the project, tagged as coming from the source generator, together with a support key. The support key is the same one the Refitter CLI sends: the first 7 characters of a SHA-256 hash of your user name and machine name. Neither name is sent, but the key isn't random, so anyone who knows both names can compute it and match it to your events. Only setting names, boolean and number values, and known enum values (such as `multipleInterfaces`) are sent. Paths, URLs, namespaces, other text values and comments are redacted and never leave the machine, and settings that Refitter doesn't recognize are only counted, never named.

The source generator itself never makes network calls. Telemetry is sent by a small MSBuild task in the package that runs after the build. It doesn't run in design-time builds (IDE), only sends when the `.refitter` files or the package version change (it keeps a stamp file in `obj`), uses a short timeout, and never fails the build.

To opt out, add the following to your `.csproj` file:

```xml
<PropertyGroup>
  <RefitterNoLogging>true</RefitterNoLogging>
</PropertyGroup>
```

Setting the `DOTNET_CLI_TELEMETRY_OPTOUT` environment variable to `1` or `true` also disables it.
