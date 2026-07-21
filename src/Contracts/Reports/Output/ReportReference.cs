namespace BIMCapabilities.Contracts.Reports.Output;

/// <summary>
/// Reference to evidence, diagnostics, or other report source artifacts.
/// </summary>
public sealed record ReportReference
{
    public required string ReferenceType { get; init; }

    public required string ReferenceId { get; init; }

    public string? Description { get; init; }
}
