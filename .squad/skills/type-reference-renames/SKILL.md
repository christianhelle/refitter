---
name: "type-reference-renames"
description: "Safely rename generated C# types with Roslyn without corrupting comments, expressions, or colliding declarations"
domain: "code-generation"
confidence: "high"
source: "manual"
---

## Context
Use this when Refitter needs a post-generation rename pass over emitted C# source, such as applying a contract suffix after NSwag has already produced declarations and references.

## Patterns
- Parse the generated source with Roslyn instead of using regex replacements.
- Collect declared type names first, then build the rename map only for declarations that:
  - do not already end with the requested suffix, and
  - do not collide with an already-declared suffixed type name.
- Rename declarations directly (`class`, `record`, `struct`, `enum`).
- Rename `IdentifierNameSyntax` / `GenericNameSyntax` only when they are in **type-reference** contexts:
  - type syntax / type arguments
  - base types
  - `typeof`, casts, declaration expressions, etc.
- Preserve trivia with `WithTriviaFrom(...)` so formatting and comments stay untouched.

## Examples
- `src\Refitter.Core\ContractTypeSuffixApplier.cs`
- Regression coverage in `src\Refitter.Tests\Examples\ContractTypeSuffixTests.cs`

## Anti-Patterns
- Regex over raw generated source.
- Renaming every `SimpleNameSyntax` regardless of context.
- Creating the rename map before checking whether `Name + suffix` is already declared in the file.
