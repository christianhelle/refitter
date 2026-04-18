# GitHub Discussion Post: Breaking Changes in v2.0.0

**Status:** DRAFT (ready for review and publication)  
**Category:** Announcements  
**Recommended Title:** Breaking Changes: Refitter v1.8.0 → v2.0.0  

---

## Discussion Body (Markdown)

# ⚠️ Breaking Changes: Refitter v2.0.0

If you're upgrading from v1.7.3 to v2.0.0, please read this carefully. We've introduced **2 breaking changes** that require action depending on how you use Refitter.

The good news? Both breaks fix serious bugs and come with straightforward migration paths.

---

## Breaking Change #1: Authentication Header Configuration Renamed

### Who's Affected
Users with custom authentication headers in `.refitter` files using the `generateAuthenticationHeader` setting.

### What Changed
The authentication setting has been **renamed and restructured** from a boolean to an enum:

```json
// ❌ OLD (v1.7.3) — no longer works
{
  "generateAuthenticationHeader": true
}

// ✅ NEW (v2.0.0) — use this instead
{
  "authenticationHeaderStyle": "Method"
}
```

Valid values for `authenticationHeaderStyle`:
- `"None"` — No authentication header generation (default)
- `"Method"` — Add `[Headers("Authorization: Bearer")]` to each method
- `"Parameter"` — Add `authorizationToken` parameter to each method

### Why It's Breaking
Old JSON keys are **silently ignored**. If you don't migrate, your `.refitter` file will use the default value (`None`), which **disables authentication header generation** even though your config says otherwise.

### Migration Steps

1. Open all your `.refitter` files
2. Find any line with `"generateAuthenticationHeader"`
3. Replace based on your use case:
   - If you had `true` → use `"authenticationHeaderStyle": "Method"` or `"Parameter"`
   - If you had `false` → use `"authenticationHeaderStyle": "None"` (or remove the line)

**Example migration:**
```json
// Before
{
  "openApiPath": "./swagger.json",
  "namespace": "MyApi",
  "generateAuthenticationHeader": true
}

// After
{
  "openApiPath": "./swagger.json",
  "namespace": "MyApi",
  "authenticationHeaderStyle": "Method"
}
```

**Evidence:** [Commit 14101a49](https://github.com/christianhelle/refitter/commit/14101a49), [PR #897](https://github.com/christianhelle/refitter/pull/897)

---

## Breaking Change #2: Source Generator No Longer Writes Disk Files

### Who's Affected
Users of the `Refitter.SourceGenerator` NuGet package who expect `.g.cs` files in the file system.

### What Changed
The Source Generator now uses Roslyn's `context.AddSource()` API instead of writing physical files to disk:

```
// ❌ OLD BEHAVIOR (v1.7.3)
dotnet build
→ Generated/RefitterClient.g.cs ✅ (file appears on disk)

// ✅ NEW BEHAVIOR (v2.0.0)
dotnet build
→ Generated/RefitterClient.g.cs (in Roslyn memory, viewed through IDE)
```

Generated code is still produced and compiled—it's just not written to the file system.

### Why It's Breaking
- If you version-control the generated `.g.cs` files, they'll no longer appear in source control
- If your build process references these files directly, that won't work
- CI/CD pipelines expecting disk files will need adjustment

### Why This Is Better
This change **fixes critical issues** that affected many users:
- **#635:** Build errors when combined with `Microsoft.Extensions.ApiDescription.Server`
- **#520:** File locking errors ("The process cannot access the file")
- **#310:** Source generator crashes on .NET 8

### Migration: Two Options

#### Option A: View Generated Code in IDE (Recommended)
No action needed on your part! Generated code is still available:
- **Visual Studio:** Right-click `.refitter` file → "View Generated Code"
- **Visual Studio Code:** Right-click `.refitter` file → "Peek Generated Code" (with Roslyn extension)
- **Rider:** Right-click `.refitter` file → "View Generated Files"

This approach is cleaner and avoids cluttering your source tree.

#### Option B: Use CLI or MSBuild for Disk Output
If you need physical files (e.g., for version control or inspection), switch to Refitter's CLI or MSBuild task:

- **CLI Tool:** `dotnet tool install -g Refitter`
  ```bash
  refitter ./swagger.json --output ./GeneratedClient.cs --namespace "MyApi"
  ```

- **MSBuild Task:** Use `Refitter.MSBuild` NuGet package
  ```xml
  <PackageReference Include="Refitter.MSBuild" Version="2.0.0" />
  ```
  Then create `.refitter` files as usual; MSBuild writes outputs to disk.

**Evidence:** [Commit f853bcf2](https://github.com/christianhelle/refitter/commit/f853bcf2), [PR #923](https://github.com/christianhelle/refitter/pull/923)

---

## Not Affected: Everything Else 🎉

These features remain **fully backward compatible**:

✅ `.refitter` file format (except auth setting above)  
✅ CLI tool commands and options  
✅ MSBuild task integration  
✅ Generated Refit interface syntax  
✅ Contract type generation  

### Plus: New Features (Opt-In)
v2.0.0 adds several new capabilities—all optional with safe defaults:

- **Property Naming:** `"propertyNamingPolicy": "PreserveOriginal"` to keep original OpenAPI names
- **Multiple Specs:** `"openApiPaths": ["./swagger1.json", "./swagger2.json"]` to merge specs
- **Contract Suffix:** `"contractTypeSuffix": "Dto"` for custom contract naming
- **AOT Compilation:** `"generateJsonSerializerContext": true` for trimmed deployments
- Bug fixes for schema aliases, multipart forms, and recursive schemas

---

## Need Help?

Reply in this thread with:
- Questions about migration
- Issues after upgrading
- Clarifications on the changes above

We're here to help!

---

## Quick Checklist for Upgrading

- [ ] Search `.refitter` files for `generateAuthenticationHeader` and migrate to `authenticationHeaderStyle`
- [ ] If using Source Generator: choose IDE viewing or switch to CLI/MSBuild
- [ ] Test code generation and compilation after upgrade
- [ ] Update any CI/CD pipelines that reference generated files

---

**Full Release Notes:** See the [CHANGELOG](https://github.com/christianhelle/refitter/blob/HEAD/CHANGELOG.md) for complete list of features and fixes.

---

## Post-Publication Notes (for moderators/maintainers)

- **Pin this post** for 2-3 weeks for maximum visibility during v2.0.0 release
- **Link from CHANGELOG.md** with "See Breaking Changes Discussion" pointer
- **Monitor thread** for common migration issues; add FAQ section if needed
- **Close or unpin** after community has upgraded (typically 4-6 weeks)
