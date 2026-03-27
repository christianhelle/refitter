# Schema Graph Visitors — Refitter / NJsonSchema

## Problem

When Refitter preprocesses `JsonSchema` graphs before code generation, a naive recursive walk can stack overflow on perfectly valid recursive models such as:

- `Node -> next -> Node`
- `Node -> children[] -> Node`
- `Dictionary<string, Node>` via `AdditionalPropertiesSchema`
- recursive `allOf` / `oneOf` / `anyOf` graphs

## Key Insight

`schema.ActualSchema` is **not** enough to make a traversal safe.

NJsonSchema's `JsonSchema.ActualSchema` uses cycle detection only inside a **single** `GetActualSchema(ref checkedSchemas)` resolution chain. If your visitor calls `schema.ActualSchema` again on each recursive step, you get a fresh checker every time and can still recurse forever at the caller level.

## Safe Pattern

For any Refitter preprocessing visitor over `JsonSchema`:

1. Resolve `actualSchema` once per node.
2. Track visited nodes by **schema instance identity** (the resolved `actualSchema` object), not by schema name or `Id`.
3. Share one visited-set across the **entire preprocessing pass**, not per root call.
4. Apply the same guard to every branch:
   - `Properties`
   - `Item`
   - `AdditionalPropertiesSchema`
   - `AllOf`
   - `OneOf`
   - `AnyOf`

## Why instance identity

- Component names are unavailable for inline schemas.
- `Id` can be null or reused.
- The preprocessing logic mutates the resolved schema object, so the resolved instance is the true unit of work.

## Refitter-specific reminder

In `CSharpClientGeneratorFactory.Create()`, schema preprocessing currently runs **before** `MapCSharpGeneratorSettings()`. That means NSwag settings such as `ExcludedTypeNames` do not automatically protect preprocessing visitors unless Refitter adds an explicit guard.

## Regression testing pattern

When adding regression coverage for schema walkers in Refitter:

1. Prefer **tiny inline OpenAPI fixtures** over large borrowed specs.
2. Cover the cycle shapes separately:
   - direct self-reference via property
   - cycle via array `items`
   - cycle via `additionalProperties`
   - mutual reference (`A -> B -> A`)
   - discriminator/`allOf` back-edge
3. If the scenario also needs `PropertyNamingPolicy.PreserveOriginal`, include raw-but-valid names like `node_id`, `next_node`, and `class` so one fixture validates both traversal safety and identifier behavior.
4. If the scenario includes `ExcludedTypeNames`, do **not** stop at string assertions. Compile the generated code with a small companion stub for the excluded type using `BuildHelper.BuildCSharp(generatedCode, stubCode)`.
5. Add at least one `.refitter`-driven parity test, because MSBuild and the source generator exercise settings-file deserialization rather than the CLI option-mapping helper.
