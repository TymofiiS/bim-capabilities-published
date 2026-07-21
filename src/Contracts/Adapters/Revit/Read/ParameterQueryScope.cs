namespace BIMCapabilities.Contracts.Adapters.Revit.Read;

/// <summary>
/// Execution scope for a parameter retrieval query.
/// </summary>
public sealed record ParameterQueryScope
{
    public required ParameterQueryScopeKind Kind { get; init; }

    public IReadOnlyList<string>? ScopeIdentifiers { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Object-level scope for parameter retrieval within a query.
/// </summary>
public sealed record ParameterObjectScope
{
    public IReadOnlyList<string>? ObjectIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyIdentifiers { get; init; }

    public IReadOnlyList<string>? FamilyTypeIdentifiers { get; init; }

    public string? ObjectKind { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Reference to a shared parameter file used by a parameter retrieval query.
/// </summary>
public sealed record ParameterSharedParameterFileReference
{
    public required string FilePath { get; init; }

    public string? FileVersion { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
