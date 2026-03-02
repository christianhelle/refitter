# Backlog — Post-Review Work Items

> Generated from codebase review session 2026-03-02.
> Assignees: Keaton (🏗️), Fenster (🔧), Hockney (🧪), McManus (⚙️)
> Priority: 🔴 Critical → 🟠 High → 🟡 Medium → 🟢 Low

---

## 🔴 Critical

### SEC-1 — Rotate hardcoded Codecov token
- **File:** `.github/workflows/codecov.yml` line 24
- **Who:** McManus
- **What:** Token is hardcoded in source. Rotate the secret, move to GitHub Actions secret, reference via `${{ secrets.CODECOV_TOKEN }}`.

---

## 🟠 High — Bugs

### BUG-1 — `GetInterfaceName` strips all capital-I characters
- **File:** `src/Refitter.Core/` — `RefitMultipleInterfaceGenerator.cs` line ~48
- **Who:** Fenster → Hockney (validate)
- **What:** `.Replace("I", string.Empty)` removes every `I` from the name, not just the leading prefix. `IInvoiceEndpoint` → `nvoceQueryParams`.
- **Fix:** Use `TrimStart` or a regex to remove only a leading `I` prefix.

### BUG-2 — `ContractsOutputFolder` falls back to a file name
- **File:** `src/Refitter/GenerateCommand.cs` line ~314
- **Who:** Fenster → Hockney (validate)
- **What:** `ContractsOutputFolder = settings.ContractsOutputPath ?? settings.OutputPath` falls back to `"Output.cs"` (a file, not a folder) when `--contracts-output` is not provided.
- **Fix:** Default to a proper folder path or derive from `OutputPath` parent directory.

### BUG-3 — `ParameterExtractor` mutates NSwag-owned collection
- **File:** `src/Refitter.Core/ParameterExtractor.cs`
- **Who:** Fenster → Hockney (validate)
- **What:** Calls `.Remove()` on `operationModel.Parameters` as a side-effect — mutating a collection owned by NSwag. Fragile and can cause silent failures.
- **Fix:** Work on a copy of the collection, not the original.

---

## 🟠 High — CI/CD

### CI-1 — `squad-ci.yml` is a non-functional placeholder
- **File:** `.github/workflows/squad-ci.yml`
- **Who:** McManus
- **What:** Contains "No build commands configured" yet runs on every PR and push to main. Either wire up real commands or delete it.

### CI-2 — No explicit `dotnet format` gate in CI
- **Who:** McManus
- **What:** `dotnet format --verify-no-changes src/Refitter.slnx` is required by convention but not enforced as a CI step. Add it to `build.yml` so formatting failures block PRs.

---

## 🟠 High — Copy-paste errors

### CODE-1 — Wrong `[Description]` attributes in `RefitGeneratorSettings`
- **File:** `src/Refitter.Core/Settings/RefitGeneratorSettings.cs`
- **Who:** Fenster
- **What:** 
  - `IncludeTags` description says "Generate a Refit interface for each endpoint" (wrong)
  - `ResponseTypeOverride` description starts with "AddAcceptHeaders dictionary" (wrong — copy-paste from another property)
- **Fix:** Write correct descriptions for each property.

---

## 🟡 Medium — Missing CLI options

### CLI-1 — `GenerateAuthenticationHeader` has no CLI option
- **File:** `src/Refitter/Settings.cs` + `GenerateCommand.cs`
- **Who:** Fenster → Hockney (test)
- **What:** Property exists in `RefitGeneratorSettings` but is not exposed as a `--generate-authentication-header` CLI flag. Asymmetric with other similar flags.

### CLI-2 — `AddContentTypeHeaders` has no CLI option
- **File:** `src/Refitter/Settings.cs` + `GenerateCommand.cs`
- **Who:** Fenster → Hockney (test)
- **What:** Same issue — core property with no CLI flag.

---

## 🟡 Medium — Test gaps

### TEST-1 — `AddContentTypeHeaders` has zero tests
- **File:** `src/Refitter.Tests/Examples/` (new file needed)
- **Who:** Hockney
- **What:** No test class exists for this setting. Add `AddContentTypeHeadersTests.cs` following the standard pattern.

### TEST-2 — Source generator tests are thin
- **File:** `src/Refitter.SourceGenerator.Tests/`
- **Who:** Hockney
- **What:** Only covers Petstore spec. Add edge-case scenarios: polymorphism, optional params, multiple interfaces, `.refitter` file with non-default settings.

### TEST-3 — No OpenAPI 3.1 spec resource
- **File:** `src/Refitter.Tests/Resources/`
- **Who:** Hockney
- **What:** Add an OpenAPI 3.1 spec and corresponding smoke test to ensure forward compatibility.

### TEST-4 — `NamingSettings` has minimal coverage
- **File:** `src/Refitter.Tests/Examples/`
- **Who:** Hockney
- **What:** Add dedicated `NamingSettingsTests.cs` covering interface name patterns, operation name transforms, and parameter naming.

### TEST-5 — `GenerateStatusCodeComments` has a single test with minimal assertions
- **File:** `src/Refitter.Tests/Examples/`
- **Who:** Hockney
- **What:** Expand to assert on specific comment formats in generated output.

---

## 🟢 Low — Architecture / Tech debt

### ARCH-1 — Source generator: sync-over-async deadlock risk
- **Who:** Keaton (design) → Fenster (implement)
- **What:** `GetAwaiter().GetResult()` in the source generator can deadlock in IDE hosts (Roslyn synchronization context). Long-term: convert to async-safe pattern or restructure.
- **Note:** Not urgent — works today — but will be a problem if IDE integration is expanded.

### ARCH-2 — Source generator compiles Core source files via `<Compile Include>`
- **Who:** Keaton (design) → Fenster (implement)
- **What:** Compiles Core twice (once in Core, once in SourceGenerator). Types can drift. Should reference the assembly instead.

### ARCH-3 — MSBuild task parses `.refitter` JSON with regex
- **Who:** McManus (investigate) → Fenster (implement)
- **What:** Regex-based JSON parsing is fragile. Replace with `System.Text.Json` or `Newtonsoft.Json`.

### ARCH-4 — `GenerateCommand.cs` is 500+ lines of mixed concerns
- **Who:** Fenster (when time allows)
- **What:** Orchestration, I/O, rendering, analytics, and settings mapping all in one class. Refactor into smaller focused classes.

### CODE-2 — Typo: `defaultNamespases` in `RefitInterfaceImports.cs`
- **Who:** Fenster
- **What:** Minor spelling typo — `defaultNamespases` should be `defaultNamespaces`.

### CI-3 — Version number hardcoded in 3 workflows
- **Who:** McManus
- **What:** Centralize version in a single source (e.g., `Directory.Build.props`) and reference it from workflows.

---

## Done ✅

*(nothing yet — review just completed)*
