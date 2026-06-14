# PRD: Deepen InterfaceGenerator

## Problem Statement

The InterfaceGenerator module is 450 lines handling interface declaration, method generation (with all the header/attribute logic), return type derivation, dynamic querystring parameter class generation, obsolete attribute handling, and parameter aggregation. The `IInterfacePartitioning` interface already exists and is a good seam for multi-interface mode, but `InterfaceGenerator` itself is shallow — it's nearly as complex as the code it generates. Method generation has many conditional paths (headers, multipart, auth, dynamic querystring, cancellation, Apizr options) that are all entangled in the `GenerateMethod` method.

## Solution

Extract `IMethodGenerator` as the seam between interface assembly and method generation. Each method generation concern (signature, attributes, return type) becomes its own adapter. The generator delegates to these adapters.

The deepened module structure:

```
IMethodGenerator (new seam)
  ├── GenerateMethodSignature(operation, interfaceName, partitioning) → string
  ├── GenerateMethodAttributes(operation, interfaceName, partitioning) → string[]
  └── GenerateReturnType(operation) → string

InterfaceGenerator (existing type, deepened)
  ├── Generate(partitioning, operations) → GeneratedCode
  └── Delegates to IMethodGenerator for method-level concerns

IMethodSignatureGenerator (new module)
  ├── Generate(operation, interfaceName, partitioning, knownIdentifiers) → (methodName, parameters)
  └── Handles: method name deduplication, parameter aggregation, optional parameter reordering

IMethodAttributeGenerator (new module)
  ├── GenerateHeaders(operation, document, settings) → string[]
  ├── GenerateMultipartAttribute(operationModel) → string?
  ├── GenerateObsoleteAttribute(operation) → string?
  └── Handles: Accept headers, Content-Type headers, Authorization headers, [Multipart], [Obsolete]

IReturnTypeGenerator (new module)
  ├── Generate(operation, settings) → string
  ├── GenerateForApiResponse(operation) → string?
  ├── GenerateForFileStream(operation) → string?
  └── Handles: return type derivation, IApiResponse wrapping, Task/IObservable selection
```

The existing `InterfaceGenerator.Generate()` method becomes a composition of method generators:

```
InterfaceGenerator.Generate(partitioning)
  → for each operation group:
      → for each operation:
          → signature = methodSignatureGenerator.Generate(...)
          → attributes = methodAttributeGenerator.Generate(...)
          → returnType = returnTypeGenerator.Generate(...)
          → emit interface with signature, attributes, return type
```

## User Stories

1. As a maintainer fixing return type derivation, I want return type logic isolated in IReturnTypeGenerator, so that I don't need to understand method signature generation to fix it.
2. As a tester, I want to test return type derivation independently of method signature generation, so that I can verify return types for all operation combinations.
3. As a developer, I want the IMethodGenerator interface to be small and stable, so that new method generation concerns can be added without changing existing code.
4. As a maintainer fixing header generation, I want header logic isolated in IMethodAttributeGenerator, so that I don't risk breaking parameter aggregation.
5. As a tester, I want to test header generation for all header combinations (Accept, Content-Type, Authorization), so that I can verify all header paths.
6. As a developer, I want the IReturnTypeGenerator interface to expose return type derivation clearly, so that I can understand what determines a method's return type.
7. As a tester, I want to test dynamic querystring parameter generation independently, so that I can verify the parameter class generation.
8. As a maintainer, I want the deletion test to pass for each generator, so that I can be confident each generator concentrates its complexity.
9. As a developer, I want the InterfaceGenerator public API to remain unchanged, so that GeneratorPipeline consumers are unaffected.
10. As a tester, I want each generator to have its own test class, so that I can verify behavior in isolation.

## Implementation Decisions

- **InterfaceGenerator remains the public type:** The class name and public API surface remain unchanged. The deepening is internal.
- **IMethodGenerator interface:** A small interface that generates method-level code. The interface has three methods: GenerateMethodSignature, GenerateMethodAttributes, and GenerateReturnType.
- **IMethodSignatureGenerator:** Handles method name deduplication (using IdentifierUtils.Counted), parameter aggregation (delegating to ParameterAggregator), and optional parameter reordering (delegating to OptionalParameterReorderer).
- **IMethodAttributeGenerator:** Handles all method-level attributes: Accept headers, Content-Type headers, Authorization headers, [Multipart], [Obsolete]. Each attribute type is a separate method.
- **IReturnTypeGenerator:** Handles return type derivation: operation response type, IApiResponse wrapping, Task/IObservable selection, file stream detection, response type override.
- **Dynamic querystring parameter generation:** The existing DynamicQuerystringParameterBuilder logic is preserved. It is called by IMethodSignatureGenerator when dynamic querystring mode is enabled.
- **No new dependencies:** All deepening uses existing types. No new libraries introduced.
- **Partitioning interface preserved:** The existing `IInterfacePartitioning` interface is the correct seam for multi-interface mode. It is not changed.

## Testing Decisions

- **What makes a good test:** Only test external behavior (generated method code), not internal generator structure. Each generator's test surface is defined by its interface.
- **Return type tests:** Tests verify return type derivation for all operation combinations: success codes, file streams, IApiResponse wrapping, Task/IObservable selection, response type override.
- **Header generation tests:** Tests verify Accept header generation, Content-Type header generation, Authorization header generation, and header combinations. Tests that ignored headers are excluded.
- **Method signature tests:** Tests verify method name deduplication, parameter aggregation, and optional parameter reordering. Tests that dynamic querystring parameter generation produces correct output.
- **Attribute generation tests:** Tests verify [Multipart] attribute generation for multipart/form-data operations. Tests that [Obsolete] attribute is generated for deprecated operations.
- **Integration tests:** Existing scenario tests under `Refitter.Tests.Scenarios` remain valid. Tests that use `RefitGenerator.CreateAsync(...).Generate()` continue to work. The generated output must remain identical.
- **Build verification:** `dotnet build -c Release src/Refitter.slnx` and `dotnet test --solution src/Refitter.slnx -c Release` must pass before commit.
- **Prior art:** Existing tests for `InterfaceGenerator` follow the pattern: OpenAPI spec → generate → assert string patterns → `BuildHelper.BuildCSharp()`. These tests remain valid after deepening.

## Out of Scope

- **Interface naming logic:** The interface naming logic (using IInterfacePartitioning) is preserved in InterfaceGenerator. It is not changed.
- **Interface documentation:** The interface documentation logic (delegating to XmlDocumentationGenerator) is preserved in InterfaceGenerator. It is not changed.
- **Parameter extraction:** The existing IParameterExtractor and IParameterTypeExtractor interfaces are preserved. They are not changed.
- **Adding new method attributes:** This PRD deepens existing attribute generation only. Adding new attributes is a separate effort.
- **Performance optimization:** This PRD does not address generation performance.

## Further Notes

- **Risk:** Low. The public API is preserved. The deepening is internal refactoring.
- **Leverage:** 450 lines → 4 modules (1 interface + 3 generators). Each generator is independently testable.
- **Deletion test:** Delete IReturnTypeGenerator → return type derivation vanishes. Delete IMethodAttributeGenerator → attribute generation vanishes. Each generator concentrates complexity that would otherwise reappear across callers.
- **The interface is the test surface:** After deepening, each generator's interface defines what tests must cover. A deep interface means fewer tests know about implementation details.
