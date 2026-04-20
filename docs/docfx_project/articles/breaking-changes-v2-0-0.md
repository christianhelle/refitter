# Breaking Changes: v2.0.0

Refitter v2.0.0 introduces **2 breaking changes** that require action if you're upgrading from v1.7.3 or earlier. Both changes fix serious bugs and include straightforward migration paths.

## Breaking Change #1: Authentication Header Configuration Renamed

### Summary
The `.refitter` authentication setting has been renamed and restructured:
- **Old:** `"generateAuthenticationHeader"` (boolean)
- **New:** `"authenticationHeaderStyle"` (enum: `None`, `Method`, `Parameter`)

### Impact
Users with custom authentication headers in their `.refitter` files must update their configuration. Old JSON keys are silently ignored, causing authentication header generation to be disabled even if your config specifies otherwise.

### Migration

**Before (v1.7.3):**
```json
{
  "openApiPath": "./swagger.json",
  "namespace": "MyApi",
  "generateAuthenticationHeader": true
}
```

**After (v2.0.0):**
```json
{
  "openApiPath": "./swagger.json",
  "namespace": "MyApi",
  "authenticationHeaderStyle": "Method"
}
```

**Valid `authenticationHeaderStyle` values:**
- `"None"` — No authentication header generation (default)
- `"Method"` — Add `[Headers("Authorization: Bearer")]` to each method
- `"Parameter"` — Add `authorizationToken` parameter to each method

### Migration Steps

1. Search all `.refitter` files for `"generateAuthenticationHeader"`
2. Replace according to your use case:
   - `"generateAuthenticationHeader": true` → `"authenticationHeaderStyle": "Method"` or `"Parameter"`
   - `"generateAuthenticationHeader": false` → `"authenticationHeaderStyle": "None"` (or remove the setting)
3. Rebuild your project to verify the change

### Related Evidence
- [Commit 14101a49](https://github.com/christianhelle/refitter/commit/14101a49)
- [PR #897: Method Level Authorization header attribute](https://github.com/christianhelle/refitter/pull/897)

---

## Breaking Change #2: Source Generator No Longer Writes Disk Files

### Summary
The `Refitter.SourceGenerator` now uses Roslyn's `context.AddSource()` API to generate code in-memory instead of writing physical `.g.cs` files to disk. Generated code is still compiled into your assembly but is not persisted to the file system.

### Impact
Users of `Refitter.SourceGenerator` who:
- Version-control generated `.g.cs` files
- Reference generated files directly in build scripts
- Expect physical files in the `./Generated` folder

will need to adjust their workflow.

### Why This Change
This fix resolves critical issues:
- **Issue #635:** Build errors when combined with `Microsoft.Extensions.ApiDescription.Server`
- **Issue #520:** File locking errors ("The process cannot access the file")
- **Issue #310:** Source generator crashes on .NET 8

### Migration: Two Options

#### Option A: View Generated Code in IDE (Recommended)

Generated code remains accessible through your IDE without cluttering the file system:

- **Visual Studio:** Right-click `.refitter` file → Select **"View Generated Code"**
- **Visual Studio Code:** Right-click `.refitter` file → Select **"Peek Generated Code"** (requires Roslyn extension)
- **Rider:** Right-click `.refitter` file → Select **"View Generated Files"**

This is the recommended approach for most users.

#### Option B: Generate Physical Files with CLI or MSBuild

If you need disk files for version control, inspection, or custom build processes:

**Using Refitter CLI:**
```bash
dotnet tool install -g Refitter
refitter ./swagger.json --output ./GeneratedClient.cs --namespace "MyApi"
```

**Using Refitter MSBuild:**
```xml
<PackageReference Include="Refitter.MSBuild" Version="2.0.0" />
```

Add `.refitter` files to your project. MSBuild generates and writes physical files to the configured `outputFolder`.

### Updating CI/CD Pipelines

If your CI/CD pipeline references generated files:

1. **Remove dependencies on generated file paths** from disk (e.g., don't copy `./Generated/**/*.g.cs`)
2. **Switch to MSBuild or CLI** if you need persistent files
3. **Update version-control patterns** to remove `.g.cs` from `.gitignore` (if using MSBuild) or to ignore them (if using Source Generator)

### Related Evidence
- [Commit f853bcf2](https://github.com/christianhelle/refitter/commit/f853bcf2)
- [PR #923: Use Roslyn context.AddSource() instead of writing files to disk](https://github.com/christianhelle/refitter/pull/923)

---

## Migration Checklist

- [ ] Update `.refitter` files: replace `generateAuthenticationHeader` with `authenticationHeaderStyle`
- [ ] If using Source Generator: choose IDE viewing or switch to CLI/MSBuild
- [ ] Rebuild and test code generation
- [ ] Update CI/CD pipelines and build scripts as needed
- [ ] Remove generated `.g.cs` files from version control (if using Source Generator)

---

## What's Not Changing

These features remain **fully backward compatible**:

- `.refitter` file format (except auth setting above)
- CLI tool commands and options
- MSBuild task integration
- Generated Refit interface syntax
- Contract type generation

---

## New Features in v2.0.0

v2.0.0 also includes several new opt-in features with safe defaults:

- **Property Naming:** `"propertyNamingPolicy": "PreserveOriginal"` to keep original OpenAPI property names
- **Multiple Specs:** `"openApiPaths": ["./swagger1.json", "./swagger2.json"]` to merge multiple specifications
- **Contract Suffix:** `"contractTypeSuffix": "Dto"` for custom contract type naming
- **AOT Support:** `"generateJsonSerializerContext": true` for ahead-of-time compilation
- Bug fixes for recursive schemas, digit-prefixed property names, and multipart forms

---

## Need Help?

If you encounter issues during migration or have questions, please:

1. Check the full [CHANGELOG](../../CHANGELOG.md) for a complete list of changes
2. Review the [.refitter file format documentation](refitter-file-format.md)
3. Open an [issue on GitHub](https://github.com/christianhelle/refitter/issues)
4. Start a [discussion on GitHub](https://github.com/christianhelle/refitter/discussions)
