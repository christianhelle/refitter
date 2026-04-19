# Breaking Changes Investigation Summary

**Requested by:** Christian Helle  
**Date:** 2026-04-17  
**Status:** COMPLETE — Ready for Discussion publication  

---

## What I Found

**2 confirmed breaking changes** from v1.7.3 to v2.0.0 requiring user action:

| Change | Severity | Affected Users | Migration Effort |
|--------|----------|---|---|
| **Auth Config Renamed** | 🟡 Medium | `.refitter` files with `generateAuthenticationHeader` | 5 min (search/replace) |
| **Source Gen No Disk Files** | 🔴 High | `Refitter.SourceGenerator` NuGet users | 10-30 min (choose IDE or CLI/MSBuild) |

Both changes **fix serious bugs** (#635, #520, #310, #520) affecting file locking and build failures.

---

## Audience & Platform

- **Target:** Developers upgrading from v1.7.3 to v2.0.0 or preview versions
- **Platform:** GitHub Discussions (Announcements category)
- **Format:** Clear impact → migration options → positive framing

---

## Deliverables

### 1. `.squad/decisions/inbox/bishop-discussion-shape.md`
Decision framework documenting:
- Problem statement
- Proposed structure (title, categories, content blocks)
- User goals met (clarity, actionability, support)
- Accuracy cross-checks vs. CHANGELOG & team consensus
- Recommended next steps

### 2. `.squad/agents/bishop/discussion-post-breaking-changes-v2.0.0.md`
**READY TO PUBLISH** — Complete Discussion post with:
- Clear breaking change explanations (Why? What changed? Migration path)
- JSON examples for `.refitter` file migrations
- Two concrete options for Source Generator users (IDE vs. CLI/MSBuild)
- Non-breaking improvements (positive framing)
- Quick checklist for upgraders
- Post-publication notes (pinning duration, FAQ tracking)

### 3. `.squad/agents/bishop/history.md` (UPDATED)
Appended learnings:
- Breaking changes investigation findings
- User audience patterns
- Documentation structure preferred by Refitter users

---

## Key Evidence

✅ **Authentication Rename:**  
- Commits: 7dbf6c0c, 14101a49  
- PR: #897 (Method Level Authorization header attribute)  
- Team Consensus: 2026-04-17 decisions.md confirms breaking change

✅ **Source Gen Disk Files:**  
- Commit: f853bcf2 (PR #923)  
- Fixes: #635 (build errors), #520 (file locking), #310 (source gen crashes)  
- Team Consensus: 2026-04-17 decisions.md confirms breaking change

✅ **Non-Breaking Improvements:**  
- 8 additive features with safe defaults
- All opt-in; backward compatible
- Verified against CHANGELOG.md

---

## Accuracy Note

**Discrepancy Resolved:**  
Dallas's audit (breaking-changes-audit-1.7.3.md) called v1.8.0 "safe to release non-breaking."  
Team consensus (decisions.md) **overrode** with "BREAKING CHANGES FOUND — requires 2.0.0."  

I used **team consensus** as authoritative source for Discussion post (2 breaking changes confirmed).

---

## Next Steps (For Christian)

1. **Review** `.squad/decisions/inbox/bishop-discussion-shape.md` for structure approval
2. **Copy markdown** from `.squad/agents/bishop/discussion-post-breaking-changes-v2.0.0.md`
3. **Create GitHub Discussion** under "Announcements" category
4. **Paste post body** and publish
5. *(Optional)* Pin for 2-3 weeks during initial v2.0.0 rollout
6. *(Optional)* Monitor thread for FAQ updates

**Time to publish:** ~5 minutes from approval  
**Post maintenance:** ~30 minutes over 4-6 weeks (monitoring for migration questions)

---

## Files Created/Updated

```
.squad/
├── decisions/inbox/
│   └── bishop-discussion-shape.md          ✅ NEW
├── agents/bishop/
│   ├── history.md                          ✅ UPDATED (appended learnings)
│   ├── breaking-changes-investigation-summary.md  ✅ NEW (this file)
│   └── discussion-post-breaking-changes-v2.0.0.md ✅ NEW (ready to publish)
```

---

**Ready for publication.** No gaps or dependencies. Team decision consensus provides all evidence needed.
