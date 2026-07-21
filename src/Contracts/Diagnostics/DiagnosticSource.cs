namespace BIMCapabilities.Contracts.Diagnostics;

/// <summary>
/// Identifies the component that produced a diagnostic record.
/// </summary>
public sealed record DiagnosticSource
{
    public required string ComponentType { get; init; }

    public string? ComponentId { get; init; }

    public string? Operation { get; init; }

    public string? Code { get; init; }
}
