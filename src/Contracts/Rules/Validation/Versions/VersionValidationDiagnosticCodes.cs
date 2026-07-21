namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Diagnostic codes produced by BIMRule version compatibility validation.
/// </summary>
public static class VersionValidationDiagnosticCodes
{
    public const string ContractVersionMissing = "ContractVersionMissing";

    public const string SupportedVersionMatch = "SupportedVersionMatch";

    public const string UnsupportedVersion = "UnsupportedVersion";

    public const string FutureVersion = "FutureVersion";

    public const string DeprecatedVersion = "DeprecatedVersion";
}
