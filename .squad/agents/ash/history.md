# Ash History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Added to the squad on 2026-04-20 as a specialist reviewer for PR #1064 review work.
- PR #1064's Roslyn rewrite fixes raw regex corruption for ContractTypeSuffix, but issue #1013 is not closed unless it also blocks suffix-target collisions like `Pet` + `PetDto`.
- For ParameterExtractor multipart fields, deduplication must use the emitted C# identifier, not the original OpenAPI property key, or issue #1018 still reproduces through sanitization collisions.

## 2026-04-20 Update: PR #1064 Review Complete

**Role Assignment:** Formal appointment as Safety Reviewer  
**Scope:** Code correctness, collision detection, identifier validation  
**Status:** Completed PR #1064 static analysis; identified 2 confirmed blockers (#1013, #1018) and multiple non-blocking edge cases

**Key Contributions:**
- Analyzed ContractTypeSuffixApplier collision risk; confirmed no duplicate-target detection
- Analyzed ParameterExtractor multipart field deduplication; confirmed uses wrong key (original vs. sanitized)
- Reviewed automated comment surface; validated blocking vs. non-blocking classifications

**Team Verdict:** NO MERGE YET — 5 blockers from 4 lanes (Bishop docs-ready only)  
**Next Steps:** Await blocker fixes + final Parker/Lambert confirmations
