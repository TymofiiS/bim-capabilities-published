namespace BIMCapabilities.Contracts.Rules;

/// <summary>
/// Identifies a rule and its versioning metadata.
/// </summary>
public sealed record BimRuleMetadata
{
    public required string RuleId { get; init; }

    public required string Name { get; init; }

    public required string RuleVersion { get; init; }

    public required string ContractVersion { get; init; }

    public string? Description { get; init; }

    public string? Domain { get; init; }

    public string? Status { get; init; }

    public string? Author { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
