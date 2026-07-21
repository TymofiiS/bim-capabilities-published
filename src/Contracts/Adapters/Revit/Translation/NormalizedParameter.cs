namespace BIMCapabilities.Contracts.Adapters.Revit.Translation;

/// <summary>
/// Platform-neutral parameter value exposed by the Revit Adapter.
/// </summary>
public sealed record NormalizedParameter
{
    public required NormalizedIdentifier Identifier { get; init; }

    public required string Name { get; init; }

    public string? Value { get; init; }

    public NormalizedParameterStorageType StorageType { get; init; }

    public bool IsSharedParameter { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
