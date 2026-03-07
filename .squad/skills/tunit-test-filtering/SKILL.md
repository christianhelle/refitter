---
name: "tunit-test-filtering"
description: "Run focused Refitter tests with TUnit treenode filters"
domain: "testing"
confidence: "high"
source: "manual"
---

## Context
Refitter test projects use TUnit on Microsoft.Testing.Platform. In this repo, `dotnet test --filter ...` is not supported by the compiled test application, so the fastest reliable way to run a tiny subset of tests is through the generated test executable and a tree-node filter.

## Pattern

### Use the compiled test executable
- Test app path: `src\Refitter.Tests\bin\Release\net10.0\Refitter.Tests.exe`
- Note: The examples use Windows-style backslashes (`\`). On Linux/macOS or in bash/zsh, use forward slashes (`/`), e.g., `src/Refitter.Tests/bin/Release/net10.0/Refitter.Tests.exe`.
- Source-generator test discovery means you should build the test project first if you added or renamed tests.

### Tree filter syntax
- Format: `/*/<Namespace>/<Class>/<Test>`
- Example:

```powershell
& 'src\Refitter.Tests\bin\Release\net10.0\Refitter.Tests.exe' `
  --treenode-filter '/*/Refitter.Tests.Examples/GenerateStatusCodeCommentsTests/Generated_Code_With_Unicode_Status_Code_Comments_Can_Build' `
  --disable-logo `
  --no-progress
```

### Verified repo-specific examples
- `/*/Refitter.Tests/XmlDocumentationGeneratorTests/Can_Generate_Method_Throws_With_Readable_Unicode_Status_Code_Comments`
- `/*/Refitter.Tests.Examples/GenerateStatusCodeCommentsTests/Generated_Code_Preserves_Readable_Unicode_In_Status_Code_Comments`
- `/*/Refitter.Tests.Examples/GenerateStatusCodeCommentsTests/Generated_Code_With_Unicode_Status_Code_Comments_Can_Build`

## Anti-Patterns
- Do **not** rely on `dotnet test --filter ...` here; the TUnit/Microsoft.Testing.Platform test app rejects that option.
- Do **not** assume OR expressions are necessary for a tiny run; separate single-test invocations are often simpler and more reliable.
