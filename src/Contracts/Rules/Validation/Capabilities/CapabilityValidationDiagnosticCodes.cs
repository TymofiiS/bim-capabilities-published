namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Diagnostic codes produced by capability compatibility validation.
/// </summary>
public static class CapabilityValidationDiagnosticCodes
{
    public const string CapabilityMissing = "CapabilityMissing";

    public const string CapabilityUnknown = "CapabilityUnknown";

    public const string CapabilityDuplicate = "CapabilityDuplicate";

    public const string CapabilityDeprecated = "CapabilityDeprecated";

    public const string ConfigurationKeyUnknown = "ConfigurationKeyUnknown";

    public const string ConfigurationPlaceholderForbidden = "ConfigurationPlaceholderForbidden";

    public const string ConfigurationValueInvalid = "ConfigurationValueInvalid";
}
