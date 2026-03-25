# Decision: Issue #967 Implementation Plan — Preserve Original Property Names

**Status:** ✅ DECISION FINALIZED  
**Date:** 2026-03-25  
**Owner:** Fenster  
**Team Consensus:** Yes (approvals in orchestration log)

---

## Decision Statement

**Feature:** Allow generated C# contract properties to preserve original OpenAPI property names instead of mandatory PascalCase conversion.

**Approved Approach:**
1. **CLI Surface:** `--property-naming-policy` enum option with 4 values: `PascalCase` (default), `Preserve`, `CamelCase`, `SnakeCase`
2. **Implementation:** Pluggable `IPropertyNameGenerator` hierarchy; inject correct generator based on policy
3. **Scope (Phase 1):** CLI only; defer `.refitter` file support to Phase 2 (source generator/MSBuild)
4. **Default Behavior:** `PascalCase` (backward compatible, no breaking changes)

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **CLI-only (Phase 1)** | `.refitter` files require polymorphic deserialization of `IPropertyNameGenerator`; defer to Phase 2 to avoid JSON schema complexity |
| **Enum-based routing** | Simpler than discriminator pattern; aligns with existing settings architecture |
| **Preserve mode rejects invalid identifiers** | Safer than silent sanitization; gives users explicit control; can extend sanitization logic in future |
| **Reserved keywords → `@` prefix** | Standard C# pattern; clearer intent than suffix |
| **Always emit `[JsonPropertyName]`** | Ensures correct serialization mapping regardless of property name choice |
| **Default = PascalCase** | Maintains backward compatibility; opt-in to new modes |

---

## Implementation Boundary

**In Scope (Phase 1):**
- New CLI option `--property-naming-policy`
- Core property name generators (Preserve, CamelCase)
- C# identifier validation & sanitization utilities
- Unit & integration tests
- README documentation

**Out of Scope (Phase 2):**
- `.refitter` file support
- Source generator integration
- MSBuild task integration
- Schema evolution for polymorphic `IPropertyNameGenerator` deserialization

---

## Files Changed (Phase 1)

**New Files (5):**
- `src/Refitter.Core/PropertyNamingPolicy.cs` — Enum
- `src/Refitter.Core/IdentifierUtils.cs` — Validation/sanitization
- `src/Refitter.Core/PreservingPropertyNameGenerator.cs` — Generator
- `src/Refitter.Core/CamelCasePropertyNameGenerator.cs` — Generator (optional)
- `src/Refitter.Tests/Examples/PropertyNamingPolicyTests.cs` — Tests

**Modified Files (6):**
- `src/Refitter/Settings.cs` — CLI option
- `src/Refitter/GenerateCommand.cs` — Mapper
- `src/Refitter.Core/Settings/RefitGeneratorSettings.cs` — Core property
- `src/Refitter.Core/CSharpClientGeneratorFactory.cs` — Generator routing
- `README.md` — Documentation
- `docs/json-schema.json` — Future compatibility hint

---

## Success Criteria (Phase 1)

- ✅ CLI option `--property-naming-policy` functional and documented
- ✅ Preserve mode generates snake_case property names with `[JsonPropertyName]` binding
- ✅ All edge cases handled: hyphens (rejected), reserved keywords (`@class`), leading digits (`_123`)
- ✅ 10+ unit tests pass; 0 regressions in existing 1415+ test suite
- ✅ Code compiles, serializes/deserializes correctly
- ✅ `dotnet format` passes

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Serialization breaks if `[JsonPropertyName]` not emitted | Always emit for all modes; guaranteed by generator code |
| Invalid identifiers cause compilation errors | Sanitization + validation in IdentifierUtils; test coverage |
| Reserved keyword collisions | Auto-escape with `@` prefix; warn on collisions |
| Backward compatibility break | Default = PascalCase; no change to existing behavior |

---

## Next Steps

1. **Fenster:** Implement Phase 1 per `.squad/implementation-plan-issue-967.md`
2. **Hockney:** Add test coverage (already specified in plan)
3. **McManus:** Monitor CI/CD (no workflow changes needed for Phase 1)
4. **Phase 2 (Future):** Schedule source generator/MSBuild integration once Phase 1 validated

---

## References

- GitHub Issue: #967
- Investigation Report: `.squad/agents/fenster/history.md` → Issue #967 Investigation
- Test Design: `.squad/skills/raw-property-names/SKILL.md`
- Implementation Plan: `.squad/implementation-plan-issue-967.md`
