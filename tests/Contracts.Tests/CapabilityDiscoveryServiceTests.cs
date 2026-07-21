using BIMCapabilities.Contracts.Rules.Validation.Capabilities;

namespace BIMCapabilities.Contracts.Tests;

public class CapabilityDiscoveryServiceTests
{
    private readonly CapabilityDiscoveryService _discovery = new();

    [Fact]
    public void GetCapabilities_enumerates_all_registered_capabilities()
    {
        var capabilities = _discovery.GetCapabilities();

        Assert.Equal(5, capabilities.Count);
        Assert.Contains(capabilities, capability => capability.CapabilityId == "parameter.existence");
        Assert.Contains(capabilities, capability => capability.CapabilityId == "naming.prefix.validation");
        Assert.Contains(capabilities, capability => capability.CapabilityId == "family.imported-cad");
        Assert.Contains(capabilities, capability => capability.CapabilityId == "report.compliance");
        Assert.Contains(capabilities, capability => capability.CapabilityId == "naming.prefix.legacy");
    }

    [Fact]
    public void GetSupportedCapabilities_excludes_deprecated_entries()
    {
        var supported = _discovery.GetSupportedCapabilities();

        Assert.Equal(4, supported.Count);
        Assert.DoesNotContain(supported, capability => capability.CapabilityId == "naming.prefix.legacy");
    }

    [Fact]
    public void GetCapability_returns_full_metadata_from_registry()
    {
        var capability = _discovery.GetCapability("parameter-engine", "parameter.existence");

        Assert.NotNull(capability);
        Assert.Equal("Parameter Existence", capability!.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(capability.Description));
        Assert.Equal("parameter-engine", capability.EngineId);
        Assert.Equal(CapabilityCompatibilityStatus.Supported, capability.Status);
        Assert.Equal(CapabilityHandlerIds.ParameterExistence, capability.HandlerId);
        Assert.NotEmpty(capability.ConfigurationSchema.Keys);
        Assert.NotEmpty(capability.Examples);
    }

    [Fact]
    public void GetCapability_returns_null_for_unknown_capability()
    {
        Assert.Null(_discovery.GetCapability("parameter-engine", "parameter.unknown"));
    }

    [Fact]
    public void Discovery_service_reads_from_registry_not_hardcoded_lists()
    {
        var customDefinition = new CapabilityDefinition
        {
            EngineId = "custom-engine",
            CapabilityId = "custom.capability",
            DisplayName = "Custom Capability",
            Description = "Custom test capability.",
            HandlerId = "handler.custom",
            Status = CapabilityCompatibilityStatus.Supported
        };

        var customDiscovery = new CapabilityDiscoveryService(
            new CapabilityRegistry([..CapabilityCatalogDefinitions.All, customDefinition]));

        var capability = customDiscovery.GetCapability("custom-engine", "custom.capability");

        Assert.NotNull(capability);
        Assert.Equal("Custom Capability", capability!.DisplayName);
    }
}
