using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;

namespace BIMCapabilities.Contracts.Tests;

public class CapabilityCompatibilityValidatorTests
{
    private readonly CapabilityCompatibilityValidator _validator = new();

    [Fact]
    public void Validate_passes_for_supported_capabilities()
    {
        var result = _validator.Validate(BimRuleTestData.CreateDemoRule());

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_detects_missing_capability_reference()
    {
        var rule = CreateRuleWithCapabilities(
            "naming-engine",
            new BimRuleCapabilityReference { AtomId = string.Empty });

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityMissing, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Error, diagnostic.Severity);
        Assert.Equal("naming-engine", diagnostic.EngineId);
    }

    [Fact]
    public void Validate_detects_unknown_capability()
    {
        var rule = CreateRuleWithCapabilities(
            "naming-engine",
            new BimRuleCapabilityReference { AtomId = "naming.unknown.atom" });

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityUnknown, diagnostic.Code);
        Assert.Equal("naming.unknown.atom", diagnostic.ActualCapability);
        Assert.Contains("naming.prefix.validation", diagnostic.ExpectedCapability);
    }

    [Fact]
    public void Validate_detects_duplicate_capability_reference()
    {
        var duplicateCapability = new BimRuleCapabilityReference { AtomId = "naming.prefix.validation" };
        var rule = BimRuleTestData.CreateDemoRule() with
        {
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = "naming-engine",
                    Order = 1,
                    Capabilities = [duplicateCapability, duplicateCapability]
                }
            ]
        };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityDuplicate, diagnostic.Code);
        Assert.Equal("naming.prefix.validation", diagnostic.CapabilityName);
    }

    [Fact]
    public void Validate_detects_deprecated_capability()
    {
        var rule = CreateRuleWithCapabilities(
            "naming-engine",
            new BimRuleCapabilityReference { AtomId = "naming.prefix.legacy" });

        var result = _validator.Validate(rule);

        Assert.True(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(CapabilityValidationDiagnosticCodes.CapabilityDeprecated, diagnostic.Code);
        Assert.Equal(ValidationSeverity.Warning, diagnostic.Severity);
        Assert.Equal("naming.prefix.legacy", diagnostic.ActualCapability);
        Assert.Equal("naming.prefix.validation", diagnostic.ExpectedCapability);
    }

    [Fact]
    public void Validate_returns_multiple_capability_errors()
    {
        var rule = BimRuleTestData.CreateDemoRule() with
        {
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = "naming-engine",
                    Order = 1,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference { AtomId = string.Empty },
                        new BimRuleCapabilityReference { AtomId = "naming.unknown.atom" }
                    ]
                }
            ]
        };

        var result = _validator.Validate(rule);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Diagnostics.Count);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CapabilityValidationDiagnosticCodes.CapabilityMissing);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CapabilityValidationDiagnosticCodes.CapabilityUnknown);
    }

    [Fact]
    public void Validate_diagnostics_include_required_fields()
    {
        var rule = CreateRuleWithCapabilities(
            "parameter-engine",
            new BimRuleCapabilityReference { AtomId = "parameter.unknown" });

        var result = _validator.Validate(rule);
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Code));
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
        Assert.Equal("parameter.unknown", diagnostic.CapabilityName);
        Assert.Equal("parameter.unknown", diagnostic.ActualCapability);
        Assert.Equal("parameter-engine", diagnostic.EngineId);
    }

    [Fact]
    public void CapabilityRegistry_resolves_registered_capabilities_deterministically()
    {
        var registry = BimRuleCapabilityRegistry.Default;

        Assert.True(registry.TryGetDefinition("naming-engine", "naming.prefix.validation", out var supported));
        Assert.NotNull(supported);
        Assert.Equal(CapabilityCompatibilityStatus.Supported, supported!.Status);

        Assert.True(registry.TryGetDefinition("naming-engine", "naming.prefix.legacy", out var deprecated));
        Assert.Equal(CapabilityCompatibilityStatus.Deprecated, deprecated!.Status);

        Assert.False(registry.TryGetDefinition("naming-engine", "naming.unknown", out _));
        Assert.Equal(5, registry.Definitions.Count);
    }

    private static BimRule CreateRuleWithCapabilities(string engineId, params BimRuleCapabilityReference[] capabilities)
    {
        return BimRuleTestData.CreateDemoRule() with
        {
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = engineId,
                    Order = 1,
                    Capabilities = capabilities
                }
            ]
        };
    }
}
