---
name: "schema-type-name-sanitization"
description: "Handle malformed OpenAPI schema names that collapse to empty generated C# type names, especially trailing-dot definition keys."
domain: "generator"
confidence: "high"
source: "observed"
---

## Context

Use this when Refitter emits a blank contract declaration (`partial class` with no name) or an empty generic like `Task<>` for a response type even though the OpenAPI schema exists.

## Patterns

- Refitter inherits schema type names from NSwag/NJsonSchema unless it explicitly injects a custom `TypeNameGenerator`.
- NJsonSchema `DefaultTypeNameGenerator` treats `.` as a segment separator via `GetLastSegment()`.
- A trailing-dot schema key such as `LookUpErnResponse.` yields an empty final segment, and the anonymous-name fallback can also return an empty string.
- Once that empty name is registered in the resolver, the failure propagates everywhere that schema type is used: DTO declarations, method return types, and file names.
- The safest Refitter-side mitigation is a narrow custom type-name generator in `CSharpClientGeneratorFactory` that only repairs empty-tail/blank-name cases while preserving default naming for normal schemas.
- If a malformed schema key normalizes to the same hint as an existing clean schema key (for example `LookUpErnResponse.` vs `LookUpErnResponse`), prefer the clean key's base name and force the malformed key onto the counted collision variant (`LookUpErnResponse2`, etc.).
- The tester-side regression shape is a minimal inline Swagger/OpenAPI fixture plus four proofs: no blank `partial class`, no `Task<>`, one concrete sanitized DTO name reused in both contract and interface signature assertions, and a compile gate.

## Examples

- Swagger/OpenAPI definition key `LookUpErnResponse.` -> generated `internal partial class` with no identifier.
- Response `$ref: "#/definitions/LookUpErnResponse."` -> generated `Task<>` instead of `Task<LookUpErnResponse>`.
- Safe normalization target: `LookUpErnResponse.` -> `LookUpErnResponse`.
- Safe collision target: clean `LookUpErnResponse` stays `LookUpErnResponse`, malformed `LookUpErnResponse.` becomes `LookUpErnResponse2`.

## Anti-Patterns

- Do not patch `RefitInterfaceGenerator` to special-case empty return types; that only masks the upstream DTO naming failure.
- Do not rewrite component/definition keys and all refs unless absolutely necessary; that is broader and riskier than fixing emitted type naming.
- Do not replace every dot blindly; preserve the current "last non-empty segment" behavior for normal dotted hints and only repair malformed empty-tail cases.
