using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Result of BIMRule contract version compatibility validation.
/// </summary>
public sealed record VersionValidationResult
{
    public IReadOnlyList<VersionValidationDiagnostic> Diagnostics { get; init; } = [];

    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != ValidationSeverity.Error);
}
