using BIMCapabilities.Contracts.Adapters.Revit.Translation;

namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Execution scope for a transaction grouping write requests.
/// </summary>
public sealed record TransactionScope
{
    public required TransactionScopeKind Kind { get; init; }

    public IReadOnlyList<NormalizedIdentifier>? TargetObjects { get; init; }

    public IReadOnlyList<string>? ScopeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
