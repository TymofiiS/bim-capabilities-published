using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Capabilities;

/// <summary>
/// Describes a capability compatibility validation outcome.
/// </summary>
public sealed record CapabilityValidationDiagnostic
{
    public required string Code { get; init; }

    public required ValidationSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? CapabilityName { get; init; }

    public string? ExpectedCapability { get; init; }

    public string? ActualCapability { get; init; }

    public string? EngineId { get; init; }
}
