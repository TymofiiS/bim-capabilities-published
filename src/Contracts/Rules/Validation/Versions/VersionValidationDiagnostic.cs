using BIMCapabilities.Contracts.Rules.Validation;

namespace BIMCapabilities.Contracts.Rules.Validation.Versions;

/// <summary>
/// Describes a version compatibility validation outcome.
/// </summary>
public sealed record VersionValidationDiagnostic
{
    public required string Code { get; init; }

    public required ValidationSeverity Severity { get; init; }

    public required string Message { get; init; }

    public string? ExpectedVersion { get; init; }

    public string? ActualVersion { get; init; }
}
