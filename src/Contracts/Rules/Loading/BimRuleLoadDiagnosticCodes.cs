namespace BIMCapabilities.Contracts.Rules.Loading;

/// <summary>
/// Diagnostic codes produced by the BIMRule loader.
/// </summary>
public static class BimRuleLoadDiagnosticCodes
{
    public const string FileNotFound = "FileNotFound";

    public const string FileEmpty = "FileEmpty";

    public const string InvalidFormat = "InvalidFormat";

    public const string DeserializationFailure = "DeserializationFailure";
}
