---
name: "generator-cleanup-audit"
description: "How to identify and sequence behavior-preserving cleanups in Refitter's generator layer without weakening regression protection."
domain: "testing"
confidence: "high"
source: "observed"
---

## Context
Use this when auditing Refitter generator code for safe cleanup work after large AI-assisted change sets. The goal is to separate true low-risk simplifications from refactors that only look simple but depend on fragile generation ordering or weak tests.

## Patterns
- Prefer cleanups that only remove redundant work or obvious copy-paste while leaving emitted output unchanged.
- In `Refitter.Core`, the safest first targets are helpers with strong public-output coverage, such as XML-doc string assembly and syntax-rewriter rename branches.
- Before touching `ParameterExtractor` or generator orchestration, verify there is compile-backed public coverage for the affected behavior, not just reflection-heavy private-method assertions.
- Treat duplicated method-emission flow across `RefitInterfaceGenerator`, `RefitMultipleInterfaceGenerator`, and `RefitMultipleInterfaceByTagGenerator` as behavior-sensitive; preserve XML docs, obsolete attributes, multipart markers, headers, overload emission, and dynamic-query wrapper ordering.

## Examples
- `src\Refitter.Core\XmlDocumentationGenerator.cs` — redundant multi-line splitting inside `AppendXmlCommentBlock()` is a low-risk cleanup.
- `src\Refitter.Core\ContractTypeSuffixApplier.cs` — repeated Visit*Declaration rename branches are a low-risk cleanup.
- `src\Refitter.Tests\ParameterExtractorPrivateCoverageTests.cs` — reflection plus `RuntimeHelpers.GetUninitializedObject` is not a sufficient safety net for production refactors.
- `src\Refitter.Tests\ParameterExtractorEdgeCaseTests.cs` — multipart tests that skip build validation need stronger public regressions before core changes.

## Anti-Patterns
- Do not trust private-reflection tests as the only guard for generator refactors.
- Do not dedupe generator pipelines just because blocks look textually similar; verify emitted ordering contracts first.
- Do not count brittle whole-file string parsing tests as equivalent to compile-backed coverage when changing output-shaping logic.
