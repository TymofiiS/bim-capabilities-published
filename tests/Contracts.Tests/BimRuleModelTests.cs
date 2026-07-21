using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Rules;

namespace BIMCapabilities.Contracts.Tests;

public class BimRuleModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = BimRuleTestData.JsonOptions;

    [Fact]
    public void BimRule_models_are_data_only_types()
    {
        var ruleTypes = typeof(BimRule).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(BimRule).Namespace);

        Assert.All(ruleTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void BimRule_can_be_constructed_with_required_properties()
    {
        var rule = BimRuleTestData.CreateDemoRule();

        Assert.Equal("STD-ARC-OPENINGS-V01", rule.Metadata.RuleId);
        Assert.Equal("STD-ARC-OPENINGS-V01", rule.Metadata.Name);
        Assert.Equal("V01", rule.Metadata.RuleVersion);
        Assert.Equal("1.0", rule.Metadata.ContractVersion);
        Assert.Equal(4, rule.Engines.Count);
        Assert.Equal("Revit", rule.Execution.TargetPlatform);
        Assert.True(rule.Report.GenerateHtmlReport);
        Assert.Single(rule.ExternalReferences!);
    }

    [Fact]
    public void BimRule_supports_json_round_trip_serialization()
    {
        var original = BimRuleTestData.CreateDemoRule();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<BimRule>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.Metadata.RuleId, roundTrip.Metadata.RuleId);
        Assert.Equal(original.Metadata.Name, roundTrip.Metadata.Name);
        Assert.Equal(original.Metadata.ContractVersion, roundTrip.Metadata.ContractVersion);
        Assert.Equal(original.Engines.Count, roundTrip.Engines.Count);
        Assert.Equal(original.Engines[0].EngineId, roundTrip.Engines[0].EngineId);
        Assert.Equal(original.Engines[0].Capabilities![0].AtomId, roundTrip.Engines[0].Capabilities![0].AtomId);
        Assert.Equal(original.Execution.ExecutionMode, roundTrip.Execution.ExecutionMode);
        Assert.Equal(original.Report.ReportTitle, roundTrip.Report.ReportTitle);
        Assert.Equal(original.ExternalReferences![0].Location, roundTrip.ExternalReferences![0].Location);
    }

    [Fact]
    public void BimRuleEngine_supports_capability_references()
    {
        var engine = new BimRuleEngine
        {
            EngineId = "parameter-engine",
            Order = 2,
            Capabilities =
            [
                new BimRuleCapabilityReference
                {
                    AtomId = "parameter.existence",
                    Configuration = new Dictionary<string, string>
                    {
                        ["parameterName"] = "FireRating"
                    }
                }
            ]
        };

        Assert.Equal("parameter.existence", engine.Capabilities![0].AtomId);
        Assert.Equal("FireRating", engine.Capabilities[0].Configuration!["parameterName"]);
    }
}
