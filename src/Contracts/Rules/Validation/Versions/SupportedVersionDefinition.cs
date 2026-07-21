namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Defines a contract version known to the current BIMCapabilities implementation.
/// </summary>
public sealed record SupportedVersionDefinition
{
    public required string ContractVersion { get; init; }

    public required VersionCompatibilityStatus Status { get; init; }
}
