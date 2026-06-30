# Refitter

Refitter generates C# REST API clients — Refit interfaces and their contracts —
from OpenAPI specifications. It ships as a CLI tool, an MSBuild task, and a C#
source generator, all driven by the same `.refitter` settings.

## Language

**Parameter list**:
The ordered set of a generated Refit method's parameters, derived from an OpenAPI
operation's path, query, header, body, and form inputs, plus any trailing
request-options or cancellation-token argument.
_Avoid_: arguments, args
