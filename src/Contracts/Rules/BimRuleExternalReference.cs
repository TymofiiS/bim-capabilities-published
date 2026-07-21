namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// References an external resource required by a rule.
/// </summary>
public sealed record BimRuleExternalReference
{
    public required string ReferenceType { get; init; }

    public required string Location { get; init; }

    public string? Purpose { get; init; }

    public bool IsRequired { get; init; } = true;

    public string? ConsumerEngine { get; init; }
}
