namespace BIMCapabilities.Contracts.Engines.Registration;

/// <summary>
/// Version information published by an engine.
/// </summary>
public sealed record EngineVersion
{
    public required string Version { get; init; }

    public string? ConfigurationSchemaVersion { get; init; }

    public string? RuntimeCompatibilityVersion { get; init; }
}
