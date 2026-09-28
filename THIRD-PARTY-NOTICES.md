# Third-party notices

Refitter.Core contains code and templates adapted from the following projects.

## Microsoft.OpenApi and OasReader

Refitter validated OpenAPI documents with [Microsoft.OpenApi](https://github.com/Microsoft/OpenAPI.NET) and
read multi-file documents for validation with [OasReader](https://github.com/christianhelle/oasreader) until it
replaced them with its own validator. To keep the validation results the same, the reading, validation rules,
walking and reference resolution logic of Microsoft.OpenApi, and the multi-file merging logic of OasReader, were
adapted into Refitter.Core (`src/Refitter.Core/Validation`).

Copyright © Microsoft Corporation (Microsoft.OpenApi); Copyright © Christian Resma Helle (OasReader)

Both projects are licensed under the MIT License, whose text is included below.

## NJsonSchema and NSwag

Refitter generated its contracts with [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) and
[NSwag](https://github.com/RicoSuter/NSwag) until it replaced them with its own OpenAPI model and code
generator. To keep the generated code the same, parts of their schema model, type resolution, naming and
C# generation logic were adapted into Refitter.Core (`src/Refitter.Core/OpenApi`,
`src/Refitter.Core/CodeGeneration/Contracts` and `src/Refitter.Core/CodeGeneration/Operations`), and their
C# Liquid templates are included in `src/Refitter.Core/CodeGeneration/Contracts/Templates`.

Copyright © Rico Suter (NJsonSchema, 2025; NSwag, 2026)

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
