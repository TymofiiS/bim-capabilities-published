using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Composition.Capabilities;

/// <summary>
/// Wires the canonical capability registry, discovery service, and handler registry.
/// </summary>
public sealed class CapabilityPlatform
{
    public CapabilityPlatform(
        CapabilityRegistry registry,
        CapabilityHandlerRegistry handlerRegistry)
    {
        ArgumentGuard.ThrowIfNull(registry);
        ArgumentGuard.ThrowIfNull(handlerRegistry);

        Registry = registry;
        Handlers = handlerRegistry;
        Discovery = new CapabilityDiscoveryService(registry);
    }

    public static CapabilityPlatform Default { get; } = CreateProduction();

    public CapabilityRegistry Registry { get; }

    public CapabilityHandlerRegistry Handlers { get; }

    public ICapabilityDiscoveryService Discovery { get; }

    public static CapabilityPlatform CreateProduction()
    {
        return new CapabilityPlatform(
            BimRuleCapabilityRegistry.Default,
            new CapabilityHandlerRegistry(CreateProductionHandlers()));
    }

    public static CapabilityPlatform Create(
        CapabilityRegistry registry,
        IReadOnlyList<IBimRuleCapabilityHandler> handlers)
    {
        return new CapabilityPlatform(registry, new CapabilityHandlerRegistry(handlers));
    }

    private static IReadOnlyList<IBimRuleCapabilityHandler> CreateProductionHandlers()
    {
        return typeof(CapabilityPlatform).Assembly
            .GetTypes()
            .Where(type =>
                typeof(IBimRuleCapabilityHandler).IsAssignableFrom(type) &&
                type is { IsAbstract: false, IsClass: true })
            .Select(type => (IBimRuleCapabilityHandler)Activator.CreateInstance(type)!)
            .OrderBy(handler => handler.HandlerId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
