using BIMCapabilities.Contracts.Engines.Registration;

namespace BIMCapabilities.Runtime.Registration;

/// <summary>
/// In-memory engine registration store for runtime composition.
/// </summary>
public sealed class RuntimeRegistrySkeleton : IRuntimeRegistry
{
    private readonly List<EngineRegistration> _registrations = [];

    public IReadOnlyList<EngineRegistration> Registrations => _registrations;

    public void Register(EngineRegistration registration)
    {
        ArgumentGuard.ThrowIfNull(registration);
        _registrations.RemoveAll(existing => string.Equals(existing.Engine.EngineId, registration.Engine.EngineId, StringComparison.OrdinalIgnoreCase));
        _registrations.Add(registration);
    }

    public EngineRegistration? FindRegistration(string engineId)
    {
        if (string.IsNullOrWhiteSpace(engineId))
        {
            return null;
        }

        return _registrations.FirstOrDefault(
            registration => string.Equals(registration.Engine.EngineId, engineId, StringComparison.OrdinalIgnoreCase));
    }
}
