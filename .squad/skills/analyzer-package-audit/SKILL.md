---
name: "analyzer-package-audit"
description: "Verify analyzer/source-generator package dependency behavior from the packed output, not just project references"
domain: "packaging"
confidence: "high"
source: "earned"
---

## Context
Use this when auditing NuGet analyzer or source-generator packages for dependency leakage, consumer upgrades, or misleading documentation. `.csproj` metadata alone is not enough because packing can rewrite the dependency surface and bundle assemblies directly into `analyzers/dotnet/cs/`.

## Patterns
- Inspect the generated `.nuspec` (or the `.nupkg` copy of it) to see the actual transitive dependency contract exposed to consumers.
- Inspect the `.nupkg` contents to distinguish:
  - **transitive package dependencies** (shown in the nuspec)
  - **bundled analyzer-time assemblies** (files under `analyzers/dotnet/cs/`)
- Compare the package's public README with the packed behavior. Inconsistent docs are a false-closure risk even when the code changed.
- For Refitter specifically:
  - `Refitter.SourceGenerator` currently bundles `OasReader.dll` as an analyzer asset
  - but still publishes `Refit` as a nuspec dependency, so consumers still get Refit transitively

## Examples
- `src/Refitter.SourceGenerator/obj/Release/Refitter.SourceGenerator.1.0.0.nuspec`
- `src/Refitter.SourceGenerator/bin/Release/Refitter.SourceGenerator.1.0.0.nupkg`
- `src/Refitter.SourceGenerator/README.md`

## Anti-Patterns
- Do **not** conclude "dependency leak fixed" solely because a `<PackageReference>` gained `PrivateAssets` in source or because one dependency disappeared from the nuspec.
- Do **not** ignore the packaged README; for NuGet users, that README is part of the shipped behavior.
