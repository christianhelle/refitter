# PRD-0004: Deepen the Code Generation Pipeline

## Problem

To understand how a single method signature is generated, you must traverse 6+ modules:

```
InterfaceGenerator.Generate()
  → IInterfacePartitioning.GetGroupKey()      // Which interface does this op belong to?
  → IInterfacePartitioning.GetMethodName()    // What's the method name?
  → IInterfacePartitioning.GetDynamicQuerystringParameterType()
  → IMethodGenerator.GenerateMethodSignature()
    → IReturnTypeGenerator.GenerateReturnType()
    → IMethodAttributeGenerator.GenerateMethodAttributes()
    → IMethodSignatureGenerator.GenerateMethodSignature()
      → IParameterExtractor.Extract() (multiple implementations)
        → ParameterAggregator (combines Route, Query, Body, Header, Form extractors)
```

### Friction points

- **Interface leak**: `InterfaceGenerator.Generate()` exposes `IInterfacePartitioning` as a parameter. Callers must know about partitioning strategies (single interface, by-endpoint, by-tag). This is internal implementation detail.
- **Shallow modules**: `ReturnTypeGenerator`, `MethodAttributeGenerator`, and `MethodSignatureGenerator` each have small interfaces focused on one aspect of method generation. Their interfaces are nearly as complex as their implementations — they're deep in isolation but shallow when composed.
- **Parameter extraction complexity**: `ParameterAggregator` combines 5 parameter extractors (Route, Query, Body, Header, Form) in a fixed order. The order is hardcoded, and the aggregator manages the dynamic querystring parameter type exchange between extractors. To understand parameter handling, you must traverse the entire extractor hierarchy.
- **Deletion test**: If you delete `InterfaceGenerator`, `MethodGenerator`, and the parameter extractor hierarchy, complexity reappears across the codebase — but only in `RefitCodeGenerator.RunPipeline()`. This signals these modules are **internal seams** of a larger deep module, not separate modules worth exposing.

### Current architecture

```
RefitCodeGenerator.Generate()
  → CSharpClientGeneratorFactory.Create()    // Creates NSwag generator with settings
  → XmlDocumentationGenerator                 // Generates XML doc comments
  → InterfaceGenerator                        // Main generation orchestration
    → GeneratorPipeline.Run()                 // Contracts → Post-processors → Serializer context → Interfaces → DI
    → IInterfacePartitioning                  // Strategy: single/by-endpoint/by-tag
    → IMethodGenerator                        // Strategy: composed of ReturnType/Attribute/Signature generators
    → IParameterExtractor                     // Strategy: Route/Query/Body/Header/Form extractors
    → ParameterAggregator                     // Combines extractors in fixed order
  → FormatSingleFile() / FormatMultipleFiles() // Post-processing
```

**`RefitCodeGenerator` interface** (current):
```csharp
string Generate(OpenApiDocument document, RefitGeneratorSettings settings);
GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings);
```

**Problem**: The interface is already deep (two methods). But the **internal seams** (partitioning, method generation, parameter extraction) are visible through `InterfaceGenerator.Generate(IInterfacePartitioning)` — callers can pass different partitioning strategies. This is a leaky abstraction.

## Proposed Solution

Deepen `RefitCodeGenerator` so its interface hides all internal seams.

### Deepened `RefitCodeGenerator` interface

```csharp
internal sealed class RefitCodeGenerator
{
    public string Generate(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        // Internal: partition operations → generate interfaces → format output
        // All internal seams hidden behind this interface
    }

    public GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        // Internal: partition operations → generate interfaces → format as multiple files
    }
}
```

**Interface**: Unchanged (still two methods). But now the **internal seams** are truly internal — no caller can pass a partitioning strategy or parameter extractor.

### Internal seams (moved inside the module)

The partitioning strategy, method generation chain, and parameter extraction all become **internal seams** — private to the module's implementation, used by its own tests, but not exposed through the interface.

```csharp
internal sealed class RefitCodeGenerator
{
    // Internal seams — not part of the interface
    private readonly IInterfacePartitioning _partitioning;
    private readonly IMethodGenerator _methodGenerator;
    private readonly IReadOnlyList<IParameterExtractor> _parameterExtractors;

    // Constructor — internal, not exposed
    internal RefitCodeGenerator()
    {
        _partitioning = ...;
        _methodGenerator = ...;
        _parameterExtractors = ...;
    }
}
```

**Key point**: Internal seams are fine. A deep module can have internal seams (private to its implementation, used by its own tests) as well as the external seam at its interface. Don't expose internal seams through the interface just because tests use them.

### Parameter extractor deepening

The `ParameterAggregator` and parameter extractor hierarchy become internal to `RefitCodeGenerator`:

```csharp
internal sealed class RefitCodeGenerator
{
    private (string Parameters, string DynamicQuerystringType) ExtractParameters(
        OpenApiOperation operation,
        OperationModel operationModel,
        RefitGeneratorSettings settings)
    {
        // Internal: use Route, Query, Body, Header, Form extractors in fixed order
        // Combine results, reorder optional parameters, add cancellation token
    }
}
```

**Interface**: No public interface for parameter extraction. Tests verify parameter handling through `Generate()` output assertions.

### Partitioning strategy consolidation

The partitioning strategy pattern (single/by-endpoint/by-tag) is a clean use of the Strategy pattern. But it should be internal to `RefitCodeGenerator`:

```csharp
internal sealed class RefitCodeGenerator
{
    private IReadOnlyCollection<GeneratedCode> GenerateInterfaces(
        OpenApiDocument document,
        RefitGeneratorSettings settings)
    {
        var partitioning = GetPartitioningStrategy(document, settings);
        var operations = document.Paths.SelectMany(...).ToList();
        var groups = operations.GroupBy(partitioning.GetGroupKey).ToList();

        // Generate interfaces for each group
        return groups.Select(group => GenerateInterface(group, partitioning, settings)).ToArray();
    }
}
```

**Interface**: No public interface for partitioning. The strategy is chosen internally based on `settings.MultipleInterfaces`.

### Test migration

Tests that currently verify partitioning by passing different `IInterfacePartitioning` implementations should instead verify through `RefitCodeGenerator.Generate()` with different `RefitGeneratorSettings.MultipleInterfaces` values.

```csharp
// Before (leaks internal seam):
var generator = new InterfaceGenerator(settings, document, nswagGenerator, docGenerator);
var result = generator.Generate(new ByEndpointInterfacePartitioning(settings));

// After (goes through interface):
var settings = new RefitGeneratorSettings { MultipleInterfaces = MultipleInterfaces.ByEndpoint };
var codeGenerator = new RefitCodeGenerator();
var result = codeGenerator.GenerateMultipleFiles(document, settings);
```

## Files changed

| File | Action | Notes |
|------|--------|-------|
| `src/Refitter.Core/RefitCodeGenerator.cs` | Modify | Move partitioning/parameter logic inside, remove `InterfaceGenerator` parameter |
| `src/Refitter.Core/InterfaceGenerator.cs` | Modify | Make constructor private, remove `IInterfacePartitioning` parameter from `Generate()` |
| `src/Refitter.Core/IInterfacePartitioning.cs` | Keep | Internal seam — used by `RefitCodeGenerator` internally |
| `src/Refitter.Core/IMethodGenerator.cs` | Keep | Internal seam — used by `RefitCodeGenerator` internally |
| `src/Refitter.Core/IParameterExtractor.cs` | Keep | Internal seam — used by `RefitCodeGenerator` internally |
| `src/Refitter.Tests/InterfaceGeneratorTests.cs` | Delete | Tests move to `RefitCodeGenerator` tests |
| `src/Refitter.Tests/RefitCodeGeneratorTests.cs` | Modify | Add partitioning tests via `RefitGeneratorSettings.MultipleInterfaces` |
| `src/Refitter.Tests/ParameterExtractorTests.cs` | Modify | Verify parameter handling through `Generate()` output |

## Benefits

### Locality

- All code generation logic is inside `RefitCodeGenerator`. Internal refactors (changing how parameters are extracted, how partitioning works) don't affect callers.
- Change concentration: partitioning logic, method generation logic, and parameter extraction logic all in one module.

### Leverage

- Callers learn one deep interface (`Generate()`/`GenerateMultipleFiles()`) instead of navigating the generation chain.
- One implementation pays back across CLI, MSBuild, and Source Generator — all call the same interface.

### Test improvements

- **Before**: Tests verify partitioning by passing different `IInterfacePartitioning` implementations. Tests verify parameter extraction by testing extractors individually.
- **After**: Tests assert on generated code content through the interface. Tests survive internal refactors (changing parameter extraction order, adding new partitioning strategies).
- **New test surface**: Test partitioning by setting `RefitGeneratorSettings.MultipleInterfaces` and asserting on generated code. Test parameter extraction by asserting on method signatures in generated code.

## Trade-offs

- **Added complexity**: Internal seams become truly internal (private constructors, private methods). Tests must go through the interface.
- **Justification**: The interface is the test surface. Tests that go through the interface survive internal refactors. Tests that peek at internal seams break when implementation changes.
- **Risk**: Low. The `RefitCodeGenerator` interface is already deep (two methods). This deepening makes internal seams truly internal, not exposed.
- **Breaking change**: Minimal. `InterfaceGenerator` constructor becomes private. Tests that directly instantiate `InterfaceGenerator` must be updated to go through `RefitCodeGenerator`.

## Testing strategy

1. **Partitioning tests**: Test `MultipleInterfaces.ByEndpoint`, `ByTag`, and default (single) via `RefitGeneratorSettings`
2. **Parameter extraction tests**: Test route, query, body, header, and form parameters via `Generate()` output
3. **Method generation tests**: Test return types, attributes, and signatures via `Generate()` output
4. **Integration tests**: Full generation with various settings (multiple interfaces, dynamic querystring, Apizr, etc.)
5. **Refactor survival tests**: Ensure tests pass after internal refactors (e.g., changing parameter extractor order)

## Notes

- This deepening **complements** PRD-0001 (deepening `RefitGenerator`). `RefitGenerator` is the **external seam** (load → filter → clean → generate). `RefitCodeGenerator` is the **internal seam** (partition → generate → format). Both should be deep.
- The partitioning strategy pattern is a good example of a deep internal seam — the strategy is chosen internally based on settings, not passed by callers.
- Parameter extraction is another good example — the order and combination of extractors is implementation detail, not part of the interface.
- This deepening makes the code generation pipeline **more testable** and **less coupled** to internal implementation details, which aligns with the overall goal of deepening the `RefitGenerator` module (PRD-0001).
