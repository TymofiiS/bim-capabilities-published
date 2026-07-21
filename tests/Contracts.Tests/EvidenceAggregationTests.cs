using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Aggregation;

namespace BIMCapabilities.Contracts.Tests;

public class EvidenceAggregationTests
{
    private static readonly JsonSerializerOptions JsonOptions = EvidenceAggregationSerialization.Options;

    [Fact]
    public void Evidence_aggregation_contracts_are_data_only_types()
    {
        var aggregationTypes = typeof(EvidenceAggregation).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(EvidenceAggregation).Namespace);

        Assert.All(aggregationTypes, type =>
        {
            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void EvidenceAggregation_can_be_constructed_with_required_properties()
    {
        var aggregation = EvidenceAggregationTestData.CreateDemoAggregation();

        Assert.Equal("aggregation-001", aggregation.AggregationId);
        Assert.Equal("collection-001", aggregation.SourceCollectionId);
        Assert.Equal("group-by-severity", aggregation.Rule.RuleId);
        Assert.Equal("Severity", aggregation.Rule.GroupBy);
        Assert.Equal("compliance-report-v1", aggregation.ProfileId);
    }

    [Fact]
    public void EvidenceAggregation_supports_json_round_trip_serialization()
    {
        var original = EvidenceAggregationTestData.CreateDemoAggregation();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EvidenceAggregation>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.AggregationId, roundTrip.AggregationId);
        Assert.Equal(original.Rule.Strategy, roundTrip.Rule.Strategy);
        Assert.Equal(original.Rule.Metadata!["profileType"], roundTrip.Rule.Metadata!["profileType"]);
    }

    [Fact]
    public void EvidenceSummary_supports_required_breakdown_dimensions()
    {
        var summary = EvidenceAggregationTestData.CreateDemoSummary();

        Assert.Equal(10, summary.TotalEvidence);
        Assert.Equal(3, summary.BySeverity![EvidenceSeverity.Error]);
        Assert.Equal(6, summary.ByCategory![EvidenceCategory.Validation]);
        Assert.Equal(6, summary.BySource!["parameter-engine"]);
        Assert.Equal(5, summary.ByTarget!["door-001"]);
        Assert.NotNull(summary.Statistics);
    }

    [Fact]
    public void EvidenceSummary_supports_json_round_trip_serialization()
    {
        var original = EvidenceAggregationTestData.CreateDemoSummary();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EvidenceSummary>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.TotalEvidence, roundTrip.TotalEvidence);
        Assert.Equal(original.BySeverity![EvidenceSeverity.Warning], roundTrip.BySeverity![EvidenceSeverity.Warning]);
        Assert.Equal(original.BySource!["naming-engine"], roundTrip.BySource!["naming-engine"]);
    }

    [Fact]
    public void EvidenceStatistics_supports_counts_totals_breakdowns_and_percentages()
    {
        var statistics = EvidenceAggregationTestData.CreateDemoStatistics();

        Assert.Equal(10, statistics.TotalCount);
        Assert.Equal(3, statistics.Counts!["error"]);
        Assert.Equal(10, statistics.Totals!["records"]);
        Assert.Equal(3, statistics.Breakdowns!["severity"]["error"]);
        Assert.Equal(30.0m, statistics.Percentages!["error"]);
    }

    [Fact]
    public void EvidenceGroup_supports_group_key_references_and_summary()
    {
        var group = EvidenceAggregationTestData.CreateDemoGroup();

        Assert.Equal("severity:error", group.GroupKey);
        Assert.Equal("Error", group.GroupName);
        Assert.Equal(3, group.EvidenceReferences.Count);
        Assert.Equal("evidence-001", group.EvidenceReferences[0]);
        Assert.Equal(3, group.Summary!.TotalEvidence);
    }

    [Fact]
    public void EvidenceAggregationResult_supports_json_round_trip_serialization()
    {
        var original = EvidenceAggregationTestData.CreateDemoResult();

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var roundTrip = JsonSerializer.Deserialize<EvidenceAggregationResult>(json, JsonOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.AggregationId, roundTrip.AggregationId);
        Assert.Equal(original.Summary.TotalEvidence, roundTrip.Summary.TotalEvidence);
        Assert.Single(roundTrip.Groups);
        Assert.Equal(original.Groups[0].GroupKey, roundTrip.Groups[0].GroupKey);
        Assert.Equal(original.Statistics!.TotalCount, roundTrip.Statistics!.TotalCount);
    }

    [Fact]
    public void EvidenceAggregationRule_required_properties_can_be_populated()
    {
        var rule = new EvidenceAggregationRule
        {
            RuleId = "group-by-engine",
            Name = "Group By Engine",
            GroupBy = "Source",
            Strategy = "GroupByEngine"
        };

        Assert.Equal("Group By Engine", rule.Name);
        Assert.Equal("Source", rule.GroupBy);
    }

    [Fact]
    public void Evidence_aggregation_contracts_do_not_reference_runtime_or_adapter_assemblies()
    {
        var contractsAssembly = typeof(EvidenceAggregation).Assembly;
        var referencedAssemblies = contractsAssembly.GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .ToArray();

        Assert.DoesNotContain("BIMCapabilities.Runtime", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Adapters.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Launchers.Revit", referencedAssemblies);
        Assert.DoesNotContain("BIMCapabilities.Engines.Report", referencedAssemblies);
    }
}
