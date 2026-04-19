# Bishop History

## Context

- User: Christian Helle
- Product: Refitter generates C# REST API clients from OpenAPI specifications using Refit.
- Stack: .NET, Refit, NSwag, Source Generator, MSBuild, Microsoft OpenAPI.NET

## Learnings

- Team initialized on 2026-04-16.
- **Issue #998 findings (2026-04-16):** Reviewed documentation vs. implementation. CLI bug in `GenerateCommand.GetOutputPath()` ignores `outputFolder` when matching default value. MSBuild task correctly predicts expected paths, but CLI doesn't honor them. Documented behavior matches intent; implementation is wrong.
- **Breaking Changes Investigation (2026-04-17):** Analyzed Dallas's audit and team consensus. Dallas missed 2 breaking changes; team overrode with correct verdict. User audience: 1.7.3 upgraders. Two breaks identified: (1) auth settings renamed (generateAuthenticationHeader → authenticationHeaderStyle), (2) source generator disk files removed (Roslyn context.AddSource()). Both fixes are high-value (bug fixes); migration paths exist. GitHub Discussion format recommended: Announcements category, clear impact statements, actionable search/replace guidance, positive framing with non-breaking improvements.
- **Documentation Pattern:** Refitter users expect JSON examples in `.refitter` context, issue/commit citations, and clear "What Changed → Why → Migration" structure. GitHub Discussion threading works well for follow-up questions.
- **Breaking Changes Finalization (2026-04-17):** Created finalized discussion draft in session files (not published to repo) as pre-release heads-up. Created permanent migration guide in docfx articles with structured Impact → Migration → Evidence format. Updated toc.yml to make guide discoverable after Source Generator. Used 2026-04-17 squad consensus as authoritative source (overriding Dallas's initial audit). Scoped guidance to Refitter-owned changes only (excluded non-breaking improvements, ecosystem guidance, and the MSBuild output path bug fix from core migration narrative). Documented team decision in decisions/inbox for squad alignment.
- **Guidance Review & Approval (2026-04-18):** Ripley completed final review of all artifacts. ✅ APPROVED FOR PUBLICATION. Identified key pattern: breaking change communication requires dual artifacts (discussion + docs). Discussion draft remains in session state pending user publication decision. Migration guide and TOC updates ready for merge.
