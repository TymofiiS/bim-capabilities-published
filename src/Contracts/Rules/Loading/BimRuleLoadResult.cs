using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Rules.Loading;

/// <summary>
/// Result of loading a .bimrule file from disk.
/// </summary>
public sealed record BimRuleLoadResult
{
    public BimRule? Rule { get; init; }

    public IReadOnlyList<BimRuleLoadDiagnostic> Diagnostics { get; init; } = [];

    public bool Success => Rule is not null && Diagnostics.Count == 0;
}
