using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Determines whether a BIMRule contract version is supported by the current implementation.
/// </summary>
public sealed class BimRuleVersionValidator : IBimRuleVersionValidator
{
    private readonly IReadOnlyList<SupportedVersionDefinition> _supportedVersions;
    private readonly string _currentSupportedVersion;

    public BimRuleVersionValidator()
        : this(BimRuleSupportedVersionCatalog.Default, BimRuleSupportedVersionCatalog.CurrentSupportedContractVersion)
    {
    }

    public BimRuleVersionValidator(
        IReadOnlyList<SupportedVersionDefinition> supportedVersions,
        string currentSupportedVersion)
    {
        ArgumentGuard.ThrowIfNull(supportedVersions);
        ArgumentGuard.ThrowIfNullOrWhiteSpace(currentSupportedVersion);

        _supportedVersions = supportedVersions;
        _currentSupportedVersion = currentSupportedVersion.Trim();
    }

    public VersionValidationResult Validate(BimRule? rule)
    {
        var contractVersion = rule?.Metadata?.ContractVersion;
        return ValidateContractVersion(contractVersion);
    }

    internal VersionValidationResult ValidateContractVersion(string? contractVersion)
    {
        if (string.IsNullOrWhiteSpace(contractVersion))
        {
            return Failure(
                VersionValidationDiagnosticCodes.ContractVersionMissing,
                ValidationSeverity.Error,
                "Contract version is required for version compatibility validation.",
                expectedVersion: _currentSupportedVersion,
                actualVersion: null);
        }

        var normalizedActual = contractVersion.Trim();
        var exactMatch = _supportedVersions.FirstOrDefault(
            definition => string.Equals(definition.ContractVersion, normalizedActual, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return exactMatch.Status switch
            {
                VersionCompatibilityStatus.Supported => Success(),
                VersionCompatibilityStatus.Deprecated => new VersionValidationResult
                {
                    Diagnostics =
                    [
                        new VersionValidationDiagnostic
                        {
                            Code = VersionValidationDiagnosticCodes.DeprecatedVersion,
                            Severity = ValidationSeverity.Warning,
                            Message = $"Contract version '{normalizedActual}' is deprecated. Expected supported version is '{_currentSupportedVersion}'.",
                            ExpectedVersion = _currentSupportedVersion,
                            ActualVersion = normalizedActual
                        }
                    ]
                },
                _ => Unsupported(normalizedActual)
            };
        }

        if (Version.TryParse(normalizedActual, out var parsedActualVersion) &&
            Version.TryParse(_currentSupportedVersion, out var parsedSupportedVersion))
        {
            if (parsedActualVersion > parsedSupportedVersion)
            {
                return Failure(
                    VersionValidationDiagnosticCodes.FutureVersion,
                    ValidationSeverity.Error,
                    $"Contract version '{normalizedActual}' is newer than the supported version '{_currentSupportedVersion}'.",
                    expectedVersion: _currentSupportedVersion,
                    actualVersion: normalizedActual);
            }
        }

        return Unsupported(normalizedActual);
    }

    private static VersionValidationResult Success()
    {
        return new VersionValidationResult();
    }

    private VersionValidationResult Unsupported(string actualVersion)
    {
        return Failure(
            VersionValidationDiagnosticCodes.UnsupportedVersion,
            ValidationSeverity.Error,
            $"Contract version '{actualVersion}' is not supported. Expected supported version is '{_currentSupportedVersion}'.",
            expectedVersion: _currentSupportedVersion,
            actualVersion: actualVersion);
    }

    private static VersionValidationResult Failure(
        string code,
        ValidationSeverity severity,
        string message,
        string? expectedVersion,
        string? actualVersion)
    {
        return new VersionValidationResult
        {
            Diagnostics =
            [
                new VersionValidationDiagnostic
                {
                    Code = code,
                    Severity = severity,
                    Message = message,
                    ExpectedVersion = expectedVersion,
                    ActualVersion = actualVersion
                }
            ]
        };
    }
}
