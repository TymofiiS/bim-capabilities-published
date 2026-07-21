namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Describes how a contract version is supported by the current implementation.
/// </summary>
public enum VersionCompatibilityStatus
{
    Supported = 0,

    Deprecated = 1
}
