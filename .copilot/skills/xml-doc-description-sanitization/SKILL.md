---
name: "xml-doc-description-sanitization"
description: "Preserve readable Unicode when copying OpenAPI response descriptions into XML doc comments"
domain: "code-generation"
confidence: "high"
source: "manual"
---

## Context
`src\Refitter.Core\XmlDocumentationGenerator.cs` writes OpenAPI response descriptions into generated XML documentation comments. Those descriptions may arrive from NSwag in JSON-escaped form (`\uXXXX`, `\"`, `\n`) even though the source OpenAPI file already contains readable Unicode.

## Pattern

### Sanitize user-sourced response descriptions only
- Decode JSON-style escape sequences before writing the text into XML docs.
- After decoding, escape XML-reserved characters (`&`, `<`, `>`) so the generated comments remain valid XML.
- Apply this only to OpenAPI-sourced response description text such as `method.ResultDescription` and `response.ExceptionDescription`.

### Preserve intentional XML doc markup
- Do **not** run the same sanitization over hardcoded fallback strings that intentionally contain `<see cref="..."/>`, `<list>`, or other XML doc tags.
- Sanitize at the insertion point where raw API text enters the XML comment output.

## Example

```csharp
var description = this.EscapeSymbols(this.DecodeJsonEscapedText(responseDescription));
```

## Anti-Patterns
- Appending `method.ResultDescription` or `response.ExceptionDescription` directly into XML comments.
- XML-escaping entire doc fragments that intentionally contain XML doc markup.
