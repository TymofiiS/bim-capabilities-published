using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Result of BIMRule capability compatibility validation.
/// </summary>
public sealed record CapabilityValidationResult
{
    public IReadOnlyList<CapabilityValidationDiagnostic> Diagnostics { get; init; } = [];

    public bool IsValid => Diagnostics.All(diagnostic => diagnostic.Severity != ValidationSeverity.Error);
}
