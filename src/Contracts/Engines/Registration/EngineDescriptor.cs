namespace BIMCapabilities.Contracts.Engines.Registration;

/// <summary>
/// Summary descriptor used to identify a registered engine.
/// </summary>
public sealed record EngineDescriptor
{
    public required string EngineId { get; init; }

    public required string Name { get; init; }

    public required EngineType EngineType { get; init; }

    public required EngineVersion Version { get; init; }

    public string? Description { get; init; }
}
