namespace Refitter.Core.Validation.Model;

internal sealed class SpecSecurityScheme : SpecReferenceable
{
    public SpecSecuritySchemeType? Type { get; set; }

    public string? Name { get; set; }

    public SpecParameterLocation? In { get; set; }

    public SpecOAuthFlows? Flows { get; set; }
}

internal enum SpecSecuritySchemeType
{
    ApiKey,
    Http,
    OAuth2,
    OpenIdConnect,
    MutualTls,
}

internal sealed class SpecOAuthFlows
{
    public SpecOAuthFlow? Implicit { get; set; }

    public SpecOAuthFlow? Password { get; set; }

    public SpecOAuthFlow? ClientCredentials { get; set; }

    public SpecOAuthFlow? AuthorizationCode { get; set; }

    public SpecOAuthFlow? DeviceAuthorization { get; set; }
}

internal sealed class SpecOAuthFlow
{
    public Uri? AuthorizationUrl { get; set; }

    public Uri? TokenUrl { get; set; }

    public Dictionary<string, string>? Scopes { get; set; }
}

/// <summary>
/// The security schemes a requirement names, each a reference to a scheme in the components.
/// </summary>
internal sealed class SpecSecurityRequirement : List<SpecReference>
{
}
