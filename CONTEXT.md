# Refitter

Generates Refit client interfaces and contracts from an OpenAPI specification.

## Language

### Reading a specification

**OpenAPI document**:
The root file or URL a generation run starts from.
_Avoid_: spec file, swagger file, input document

**Referenced file**:
A file or URL that an OpenAPI document reaches through an external `$ref`, directly or through another referenced file.
_Avoid_: external document, child document

**Document set**:
An OpenAPI document together with every referenced file it reaches, each read exactly once and already checked against the reference policy. Generation and validation resolve references only from the document set.
_Avoid_: snapshot, bundle, multi-file spec

**Reference policy**:
The rules deciding which referenced files a document set may contain: whether remote references are allowed, and the directory local references must stay inside.
_Avoid_: reference guard rules, security settings
