using BIMCapabilities.Contracts.Engines.Registration;

namespace BIMCapabilities.Runtime.Registration;

/// <summary>
/// Maintains registered engines available to the runtime.
/// </summary>
public interface IRuntimeRegistry
{
    IReadOnlyList<EngineRegistration> Registrations { get; }

    void Register(EngineRegistration registration);

    EngineRegistration? FindRegistration(string engineId);
}
