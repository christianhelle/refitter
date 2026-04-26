# Coverage baseline scan

## When to use

Use this when the team needs a safe, evidence-first testing baseline before cleanup or refactoring.

## Steps

1. Run the established repo validation commands first:
   - `dotnet restore src\Refitter.slnx`
   - `dotnet build -c Release src\Refitter.slnx --no-restore`
   - `dotnet test -c Release src\Refitter.slnx --no-build`
   - `dotnet format --verify-no-changes src\Refitter.slnx --no-restore`
2. Run the existing coverage command from `.github\workflows\codecov.yml`:
   - `dotnet test --project src\Refitter.Tests\Refitter.Tests.csproj -c Release --coverage --coverage-output coverage.cobertura.xml --coverage-output-format xml`
3. Read `src\Refitter.Tests\bin\Release\net10.0\TestResults\coverage.cobertura.xml`.
4. Separate real repo files from generated proof artifacts under `.test-work\`.
5. Rank low-risk targets by:
   - existing focused tests already present,
   - narrow uncovered branches,
   - no need for behavior changes.
6. Call out external-network tests separately so cleanup work does not confuse environment failures with regressions.

## Refitter-specific heuristics

- Treat the Codecov command as the authoritative baseline unless the workflow changes.
- `RuntimeProof.dll` coverage can dip because it includes generated files under `.test-work\`; do not let that distract from real repo source priorities.
- Good first coverage targets tend to be:
  - `ContractTypeSuffixApplier`
  - `SchemaCleaner`
  - `CSharpClientGeneratorFactory`
  - `XmlDocumentationGenerator`
  - `IdentifierUtils`
  - CLI `Settings`
- Known network-sensitive tests live in:
  - `OpenApiDocumentFactoryTests`
  - `SwaggerPetstoreTests`
  - `SwaggerPetstoreApizrTests`
  - `Examples\OpenApiUrlTests`
