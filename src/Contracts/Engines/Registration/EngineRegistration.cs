namespace BIMCapabilities.Contracts.Engines.Registration;

/// <summary>
/// Record of an engine registered for runtime orchestration.
/// </summary>
public sealed record EngineRegistration
{
    public required EngineDefinition Engine { get; init; }

    public IReadOnlyDictionary<string, string>? RegistrationMetadata { get; init; }

    public DateTimeOffset RegisteredAt { get; init; }
}
