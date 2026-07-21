using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities;

public sealed class CapabilityHandlerRegistry
{
    private readonly IReadOnlyDictionary<string, IBimRuleCapabilityHandler> _handlersById;

    public CapabilityHandlerRegistry(IReadOnlyList<IBimRuleCapabilityHandler> handlers)
    {
        ArgumentGuard.ThrowIfNull(handlers);

        _handlersById = handlers
            .GroupBy(handler => handler.HandlerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        Handlers = handlers;
    }

    public IReadOnlyList<IBimRuleCapabilityHandler> Handlers { get; }

    public IBimRuleCapabilityHandler Resolve(string handlerId)
    {
        ArgumentGuard.ThrowIfNullOrWhiteSpace(handlerId);

        if (_handlersById.TryGetValue(handlerId, out var handler))
        {
            return handler;
        }

        throw new InvalidOperationException($"No capability handler is registered for handler id '{handlerId}'.");
    }

    public bool TryResolve(string handlerId, out IBimRuleCapabilityHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            handler = null;
            return false;
        }

        return _handlersById.TryGetValue(handlerId, out handler);
    }
}
