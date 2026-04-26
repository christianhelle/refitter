---
name: "source-generator-safety-review"
description: "High-signal review checks for Refitter source-generator and adjacent code-generation safety"
domain: "code-generation"
confidence: "high"
source: "observed"
---

## Context
Use this when reviewing Refitter's source generator, generator-adjacent naming code, or other AI-assisted cleanup proposals in the emission pipeline.

## Patterns
- Verify `AddSource()` hint-name uniqueness against the **full `.refitter` identity**, not just a parent directory or friendly base name.
- Treat `AdditionalText.GetText(...)` as nullable input and require a diagnostic path for unreadable or malformed additional files.
- Prefer prefix-aware identifier shaping over blanket string replacement; removing the interface prefix `I` should only touch the leading interface marker, not every `I` in the generated name.
- When OpenAPI titles feed generated identifiers, sanitize generic-looking characters such as `<` and `>` in addition to the usual filesystem and punctuation set.

## Examples
- `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` — `CreateUniqueHintName(...)` should disambiguate same-directory `.refitter` files that share an output filename.
- `src\Refitter.SourceGenerator\RefitterSourceGenerator.cs` — `file.GetText(cancellationToken)` should not be null-forgiven without a diagnostic fallback.
- `src\Refitter.Core\RefitMultipleInterfaceGenerator.cs` — derive `QueryParams` names without stripping interior `I` characters from interface names.
- `src\Refitter.Core\IdentifierUtils.cs` — keep the sanitization set aligned with C# identifier validity for OpenAPI-title-derived names.

## Anti-Patterns
- Comments or XML docs that promise uniqueness while the implementation only hashes a directory-level value.
- Cleanup patches that replace semantic naming logic with broad `string.Replace(...)` chains.
- Treating source-generator input read failures as impossible.
