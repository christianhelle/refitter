# PRD-0005: Separate Document Processing from Code Generation

## Problem

The pipeline module (`RefitPipeline` — now absorbed into `RefitGenerator`) owns document loading, filtering, and schema cleaning. Each sub-step is a separate module, but they're composed inline with no seam between them:

```
RefitGenerator.CreateAsync(settings)
  → OpenApiDocumentFactory.CreateAsync(path)     // Load from file/URL
  → RefitDocumentFilter.FilterByTags(doc, tags)  // Filter by tags
  → RefitDocumentFilter.FilterByPath(doc, paths) // Filter by path
  → SchemaCleaner.Clean(doc, ...)                // Remove unreferenced schemas
  → RefitCodeGenerator.Generate(doc, settings)   // Generate code
```

### Friction points

- **No seam between loading and processing**: `OpenApiDocumentFactory` (loading) and `RefitDocumentFilter` (filtering) are separate modules but always called together. There's no abstraction between "get me an OpenAPI doc" and "give me a processed doc."
- **Heavy dependency for comparison**: `DocumentEquivalenceComparer` (304 lines) uses `Newtonsoft.Json` for canonical JSON comparison — a heavy dependency for what is essentially a comparison utility. It's used only by `DocumentMerger` for merge conflict detection.
- **Testing coupling**: Tests that verify filtering/cleaning must construct real OpenAPI files on disk (via `SwaggerFileHelper`) because `SchemaCleaner` and `RefitDocumentFilter` work on `OpenApiDocument` objects that are expensive to create.
- **In-process dependencies**: `RefitDocumentFilter` and `SchemaCleaner` are in-process (pure computation, no I/O) but are tested through the full pipeline (load → filter → clean → generate). This means filtering tests require file I/O.

### Deletion test

If you delete `RefitDocumentFilter` and `SchemaCleaner` as separate modules, complexity reappears in `RefitGenerator.CreateAsync()` — the filtering and cleaning logic would be inline. This signals these modules are **earning their keep** as a consolidation point for document processing.

## Current Architecture

```
RefitGenerator.CreateAsync(settings)
  → OpenApiDocumentFactory.CreateAsync(path)
    → DocumentLoader.LoadAsync(path)              // File/URL loading, YAML/JSON
    → DocumentMerger.Merge(documents)             // Merge multiple docs
      → DocumentEquivalenceComparer.Compare()     // Canonical JSON comparison (Newtonsoft.Json)
  → RefitDocumentFilter.FilterByTags(doc, tags)   // Remove non-matching operations
  → RefitDocumentFilter.FilterByPath(doc, paths)  // Remove non-matching paths
  → SchemaCleaner.Clean(doc, ...)                 // Remove unreferenced schemas
  → RefitCodeGenerator.Generate(doc, settings)    // Generate code
```

**Dependencies**:
- **Remote but owned**: `DocumentLoader` loads from file/URL (your code, across a network boundary)
- **In-process**: `RefitDocumentFilter`, `SchemaCleaner` (pure computation, no I/O)
- **True external**: `NSwag` types (third-party, `OpenApiDocument`)

## Proposed Solution

Define a port at the seam between document processing and code generation. The deepened module exposes: "give me a document + filters, get back a processed document."

### New module: `IDocumentProcessor` (port)

```csharp
public interface IDocumentProcessor
{
    /// <summary>
    /// Processes an OpenAPI document by applying filters and schema cleaning.
    /// Returns a new document without mutating the input.
    /// </summary>
    OpenApiDocument Process(
        OpenApiDocument document,
        FilterConfig filterConfig,
        SchemaConfig schemaConfig);
}
```

**Interface**: One method. Takes a document and configuration, returns a processed document.

**Error modes**: Throws on invalid configuration (e.g., invalid regex patterns).

**Ordering**: Sequential — filter by tags → filter by path → clean schemas.

**Config**: `FilterConfig` and `SchemaConfig` from `RefitGeneratorSettings`.

### Adapter 1: `InMemoryDocumentProcessor` (production — in-memory)

- Takes an `OpenApiDocument` (already loaded) and applies filters/cleaning
- Uses `RefitDocumentFilter` and `SchemaCleaner` internally
- Used by `RefitGenerator` for in-memory processing

### Adapter 2: `FileDocumentProcessor` (production — file/URL loading)

- Loads from file/URL, then applies filters/cleaning
- Wraps `OpenApiDocumentFactory` + `InMemoryDocumentProcessor`
- Used by `RefitGenerator.CreateAsync()` as the default

### Adapter 3: `TestDocumentProcessor` (test)

- In-memory implementation for testing
- Returns mock `OpenApiDocument` or applies filters to a test document
- No file I/O needed

### DocumentEquivalenceComparer optimization

`DocumentEquivalenceComparer` (304 lines) uses `Newtonsoft.Json` for canonical JSON comparison. This could be deepened to use a lighter comparison mechanism:

```csharp
public interface IDocumentEquivalenceComparer
{
    bool AreEquivalent(OpenApiDocument doc1, OpenApiDocument doc2);
}

public sealed class StructuralEquivalenceComparer : IDocumentEquivalenceComparer
{
    // Compare OpenAPI documents structurally (schema-by-schema, path-by-path)
    // without serializing to JSON first
}
```

**Benefit**: Eliminates `Newtonsoft.Json` dependency for comparison. Uses `NJsonSchema` types directly for comparison.

## Files changed

| File | Action | Notes |
|------|--------|-------|
| `src/Refitter.Core/IDocumentProcessor.cs` | New | Port definition |
| `src/Refitter.Core/InMemoryDocumentProcessor.cs` | New | In-memory adapter |
| `src/Refitter.Core/FileDocumentProcessor.cs` | New | File/URL loading adapter |
| `src/Refitter.Core/IDocumentEquivalenceComparer.cs` | New | Comparison port |
| `src/Refitter.Core/StructuralEquivalenceComparer.cs` | New | Lightweight comparison adapter |
| `src/Refitter.Core/DocumentEquivalenceComparer.cs` | Modify | Implement `IDocumentEquivalenceComparer` (deprecated) |
| `src/Refitter.Core/RefitGenerator.cs` | Modify | Use `IDocumentProcessor` instead of inline filtering |
| `src/Refitter.Tests/DocumentProcessorTests.cs` | New | Test document processing adapters |
| `src/Refitter.Tests/RefitDocumentFilterTests.cs` | Modify | Test filtering through `IDocumentProcessor` |
| `src/Refitter.Tests/SchemaCleanerTests.cs` | Modify | Test cleaning through `IDocumentProcessor` |

## Benefits

### Locality

- Document processing logic concentrates in `IDocumentProcessor` adapters
- Schema cleaning, filtering, and merging are tested together through one interface
- Changes to filtering/cleaning logic don't affect code generation

### Leverage

- The port enables swapping document sources (file, URL, in-memory) without changing the pipeline
- One interface pays back across: `RefitGenerator` (production), tests (in-memory), and future adapters (e.g., API-based loading)

### Test improvements

- **Before**: Tests verify filtering/cleaning by constructing real OpenAPI files on disk. Each test requires file I/O.
- **After**: Tests use `TestDocumentProcessor` with in-memory documents. No file I/O needed.
- **New test surface**: Test filtering logic independently of loading. Test schema cleaning independently of filtering. Test merging independently of comparison.

## Trade-offs

- **Added complexity**: One new interface and multiple adapters for logic that works today.
- **Justification**: The seam is real — document loading (I/O) and document processing (in-process) are different concerns that should be separable. Tests need to verify processing without loading.
- **Risk**: Medium. The `DocumentEquivalenceComparer` optimization (replacing Newtonsoft.Json with structural comparison) is a larger change that requires careful testing.
- **Breaking change**: Minimal. `RefitGenerator` uses `IDocumentProcessor` internally. External callers (CLI, MSBuild, Source Generator) interact with the same `RefitGenerator.CreateAsync()` interface.

## Testing strategy

1. **Document processor tests**: Test `InMemoryDocumentProcessor` with various filter combinations
2. **Document processor tests**: Test `FileDocumentProcessor` with file and URL inputs
3. **Equivalence comparison tests**: Test `StructuralEquivalenceComparer` against `DocumentEquivalenceComparer` for correctness
4. **Filtering tests**: Test tag and path filtering independently of loading
5. **Schema cleaning tests**: Test schema cleaning independently of filtering
6. **Integration tests**: Full pipeline (load → filter → clean → generate) to verify end-to-end behavior

## Notes

- This deepening **complements** PRD-0001 (deepening `RefitGenerator`) and PRD-0004 (deepening code generation). It adds a seam between document processing and code generation, enabling testable document processing without file I/O.
- The `DocumentEquivalenceComparer` optimization is lower priority — it's a correctness concern (structural comparison must match canonical JSON comparison) and a performance concern (eliminating Newtonsoft.Json serialization).
- This deepening makes document processing **more testable** and **less coupled** to file I/O, which aligns with the overall goal of deepening the `RefitGenerator` module (PRD-0001).
- The port enables future adapters (e.g., API-based loading, cached loading) without changing `RefitGenerator`.
