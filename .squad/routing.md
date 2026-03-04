# Routing Rules — Refitter Squad

## Signal → Agent Mapping

| Signal | Route To |
|--------|----------|
| Architecture, design decisions, PR review, scope questions | Keaton |
| Core library changes (`Refitter.Core/`) | Fenster |
| CLI changes (`Refitter/`) | Fenster |
| Source generator (`Refitter.SourceGenerator/`) | Fenster |
| MSBuild task (`Refitter.MSBuild/`) | McManus |
| Unit tests (`Refitter.Tests/`) | Hockney |
| Source generator tests (`Refitter.SourceGenerator.Tests/`) | Hockney |
| CI/CD workflows (`.github/workflows/`) | McManus |
| Release, versioning, changelog | McManus |
| Build failures, packaging issues | McManus |
| Code generation correctness, Refit interface quality | Hockney (validate) + Fenster (fix) |
| New CLI options / settings | Fenster (implement) + Hockney (test) |
| OpenAPI spec support, NSwag integration | Fenster |
| Documentation (`README.md`, `docs/`) | Keaton (review) + Fenster (content) |
| Multi-domain / "Team" requests | Keaton + Fenster + Hockney in parallel |
| Session logging, decisions | Scribe |
| Work queue, GitHub issues | Ralph |
