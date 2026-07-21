namespace BIMCapabilities.Contracts.Engines.Registration;

/// <summary>
/// Full definition of an engine and its published capabilities.
/// </summary>
public sealed record EngineDefinition
{
    public required string EngineId { get; init; }

    public required string Name { get; init; }

    public required EngineVersion Version { get; init; }

    public required EngineType EngineType { get; init; }

    public required IReadOnlyList<EngineCapability> Capabilities { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
