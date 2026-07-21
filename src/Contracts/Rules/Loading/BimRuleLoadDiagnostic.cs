namespace BIMCapabilities.Contracts.Rules.Loading;

/// <summary>
/// Describes a loader outcome without making validation or compatibility decisions.
/// </summary>
public sealed record BimRuleLoadDiagnostic
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public string? FilePath { get; init; }
}
