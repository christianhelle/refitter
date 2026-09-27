namespace Refitter.Core.Validation.Model;

internal sealed class SpecSchema : SpecReferenceable
{
    public SpecSchema? Items { get; set; }

    public SpecSchema? Not { get; set; }

    public List<SpecSchema>? AllOf { get; set; }

    public List<SpecSchema>? AnyOf { get; set; }

    public List<SpecSchema>? OneOf { get; set; }

    public Dictionary<string, SpecSchema?>? Properties { get; set; }

    public SpecSchema? AdditionalProperties { get; set; }

    public SpecDiscriminator? Discriminator { get; set; }

    public SpecExternalDocs? ExternalDocs { get; set; }

    public HashSet<string>? Required { get; set; }
}

internal sealed class SpecDiscriminator
{
    public string? PropertyName { get; set; }

    public Dictionary<string, SpecSchema>? Mapping { get; set; }
}
