---
name: "aot-json-serializer-context"
description: "Generate Refitter AOT serializer contexts without regex-only type discovery"
domain: "code-generation"
confidence: "high"
source: "earned"
---

## Context
Use this skill when Refitter needs to emit `JsonSerializerContext` code for generated contracts. The old regex-only approach missed namespaces, nested types, and closed generic usages, and could emit attributes in the wrong place.

## Patterns

### Parse generated contracts with Roslyn
- Use `CSharpSyntaxTree.ParseText()` over the generated contracts source.
- Collect declared contract types from `BaseTypeDeclarationSyntax`.
- Track namespace + containing-type path so nested contracts are emitted as `Outer.Inner`.

### Register only valid serializer targets
- Add all non-generic declared contract types.
- For generic contracts, register only **closed** usages found in `TypeSyntax` nodes (for example `Envelope<Pet>`), never the open declaration (`Envelope<T>`).
- Skip usages that still reference in-scope type parameters.

### Emit the context in the contracts namespace
- Wrap the generated context in `namespace {ContractsNamespace ?? Namespace}`.
- Place `[JsonSerializable(typeof(...))]` attributes directly on the partial context class, not before the namespace declaration.
- Derive the context name from `Naming.InterfaceName`, stripping a conventional leading `I` (`IUsersApi` -> `UsersApiSerializerContext`), and fall back to `NamingSettings.DefaultInterfaceName` if the name is blank.

### Wire both generation paths
- Single-file output: append the serializer context after generated contracts.
- Multi-file output: emit a dedicated `{ContextName}.cs` file.

## Examples

- `src/Refitter.Core/JsonSerializerContextGenerator.cs`
- `src/Refitter.Core/RefitGenerator.cs`
- `src/Refitter.Tests/JsonSerializerContextGeneratorTests.cs`
- `src/Refitter.Tests/Scenarios/GenerateJsonSerializerContextTests.cs`

## Anti-Patterns

- Do **not** regex-scan emitted C# for type names; this loses namespace/nesting/generic information.
- Do **not** register open generic declarations with `[JsonSerializable]`.
- Do **not** emit serializer-context attributes outside the namespace/class declaration they belong to.
