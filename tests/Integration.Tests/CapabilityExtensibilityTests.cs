using BIMCapabilities.Composition.Capabilities;
using BIMCapabilities.Composition.Interpretation;
using BIMCapabilities.Contracts.Rules;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities;
using BIMCapabilities.Contracts.Rules.Validation.Capabilities.Handlers;

namespace BIMCapabilities.Integration.Tests;

public class CapabilityExtensibilityTests
{
    [Fact]
    public void Test_capability_is_discovered_and_resolved_without_interpreter_changes()
    {
        var platform = CreatePlatformWithTestCapability();
        var rule = CreateRuleWithTestCapability();

        var discovered = platform.Discovery.GetCapability("test-engine", "test.capability");
        Assert.NotNull(discovered);
        Assert.Equal("Test Capability", discovered!.DisplayName);
        Assert.Equal(TestCapabilityHandler.HandlerIdValue, discovered.HandlerId);

        var plan = BimRuleExecutionInterpreter.Interpret(rule, platform);

        Assert.Contains("test.capability.executed", plan.InterpretationMarkers);
    }

    [Fact]
    public void Every_supported_capability_has_a_registered_handler()
    {
        foreach (var capability in CapabilityPlatform.Default.Discovery.GetSupportedCapabilities())
        {
            Assert.True(
                CapabilityPlatform.Default.Handlers.TryResolve(capability.HandlerId, out var handler),
                $"Missing handler for capability '{capability.CapabilityId}'.");
            Assert.Equal(capability.EngineId, handler!.EngineId);
            Assert.Equal(capability.CapabilityId, handler.CapabilityId);
        }
    }

    [Fact]
    public void Supported_capabilities_answer_what_is_supported()
    {
        var supported = CapabilityPlatform.Default.Discovery.GetSupportedCapabilities();

        Assert.Equal(4, supported.Count);
        Assert.All(supported, capability =>
        {
            Assert.False(string.IsNullOrWhiteSpace(capability.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(capability.Description));
            Assert.False(string.IsNullOrWhiteSpace(capability.HandlerId));
        });
    }

    private static CapabilityPlatform CreatePlatformWithTestCapability()
    {
        var testDefinition = new CapabilityDefinition
        {
            EngineId = TestCapabilityHandler.EngineIdValue,
            CapabilityId = TestCapabilityHandler.CapabilityIdValue,
            DisplayName = "Test Capability",
            Description = "Test-only capability used to prove platform extensibility.",
            Status = CapabilityCompatibilityStatus.Supported,
            HandlerId = TestCapabilityHandler.HandlerIdValue,
            ConfigurationSchema = new CapabilityConfigurationSchema
            {
                Keys =
                [
                    new CapabilityConfigurationKey
                    {
                        Key = "test.marker",
                        Description = "Marker value recorded during interpretation.",
                        Required = false
                    }
                ]
            }
        };

        var registry = new CapabilityRegistry(
        [
            ..CapabilityCatalogDefinitions.All,
            testDefinition
        ]);

        var handlers = CapabilityPlatform.Default.Handlers.Handlers
            .Append(new TestCapabilityHandler())
            .ToArray();

        return CapabilityPlatform.Create(registry, handlers);
    }

    private static BimRule CreateRuleWithTestCapability()
    {
        return new BimRule
        {
            Metadata = new BimRuleMetadata
            {
                RuleId = "TEST-CAPABILITY-RULE",
                Name = "TEST-CAPABILITY-RULE",
                RuleVersion = "V01",
                ContractVersion = "1.0",
                Description = "Rule used to prove test capability extensibility.",
                Domain = "Test",
                Status = "Approved",
                Author = "Integration Test",
                CreatedAt = DateTimeOffset.Parse("2026-06-20T00:00:00+00:00")
            },
            Engines =
            [
                new BimRuleEngine
                {
                    EngineId = TestCapabilityHandler.EngineIdValue,
                    Order = 1,
                    Capabilities =
                    [
                        new BimRuleCapabilityReference
                        {
                            AtomId = TestCapabilityHandler.CapabilityIdValue,
                            Configuration = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["test.marker"] = "proof"
                            }
                        }
                    ]
                }
            ],
            Execution = new BimRuleExecution
            {
                TargetPlatform = "Revit",
                ExecutionMode = "Validation",
                ValidationEnabled = true
            },
            Report = new BimRuleReport
            {
                GenerateHtmlReport = false,
                GenerateJsonReport = false
            }
        };
    }
}

internal sealed class TestCapabilityHandler : IBimRuleCapabilityHandler
{
    internal const string HandlerIdValue = "handler.test.capability";
    internal const string EngineIdValue = "test-engine";
    internal const string CapabilityIdValue = "test.capability";

    public string HandlerId => HandlerIdValue;

    public string EngineId => EngineIdValue;

    public string CapabilityId => CapabilityIdValue;

    public void ContributeToExecutionPlan(
        BimRuleCapabilityInterpretationContext context,
        IBimRuleExecutionPlanBuilder builder)
    {
        builder.AddInterpretationMarker("test.capability.executed");
        builder.AddCategory("TestCategory");
    }
}
