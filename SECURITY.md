# Security Policy

## Supported Versions

Only the latest minor release receives security fixes. Fixes ship in a new patch or minor release; older versions are not backported.

| Version | Supported          |
| ------- | ------------------ |
| 2.4.x   | :white_check_mark: |
| < 2.4   | :x:                |

This applies to every distribution: the `refitter` CLI tool, `Refitter.MSBuild`, `Refitter.SourceGenerator` and `Refitter.Core`.

## Reporting a Vulnerability

**Do not open a public issue for security vulnerabilities.**

Report privately through GitHub's private vulnerability reporting:

https://github.com/christianhelle/refitter/security/advisories/new

Please include:

- Refitter version and distribution (CLI, MSBuild, Source Generator or Core)
- A minimal OpenAPI specification and/or `.refitter` file that reproduces the issue
- The impact (what an attacker gains) and a proof of concept, if available

## Scope

Refitter turns OpenAPI specifications into C# code that gets compiled into your application. A specification from an untrusted source is the main attack surface.

In scope:

- Code injection into generated C# (unescaped attribute arguments, string literals, identifiers, etc.)
- `$ref` resolution leading to SSRF, remote file inclusion or local file disclosure
- Path traversal when writing generated files
- Code execution at build time through the MSBuild task or Source Generator

Out of scope:

- Vulnerabilities in dependencies such as NSwag or Refit (please report those upstream)
- Issues that require the attacker to already control the build, the `.refitter` settings file or custom templates

## What to Expect

Refitter is maintained by one person, so timelines are best-effort:

- Acknowledgement within 7 days
- Triage and a fix as soon as practical, depending on severity
- Coordinated disclosure through a GitHub Security Advisory, with a CVE where applicable
- Credit for the reporter in the advisory and the README contributors list, unless you prefer to stay anonymous

## Past Advisories

Published advisories are listed at https://github.com/christianhelle/refitter/security/advisories
