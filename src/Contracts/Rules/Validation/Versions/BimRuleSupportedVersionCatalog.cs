namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Default supported BIMRule contract versions for the current implementation.
/// </summary>
public static class BimRuleSupportedVersionCatalog
{
    public const string CurrentSupportedContractVersion = "1.0";

    public static IReadOnlyList<SupportedVersionDefinition> Default { get; } =
    [
        new SupportedVersionDefinition
        {
            ContractVersion = "1.0",
            Status = VersionCompatibilityStatus.Supported
        },
        new SupportedVersionDefinition
        {
            ContractVersion = "0.9",
            Status = VersionCompatibilityStatus.Deprecated
        }
    ];
}
