using NSwag;

namespace Refitter.Core;

internal record OpenApiOperationInfo(string Path, string Verb, OpenApiOperation Operation);
