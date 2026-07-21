using BIMCapabilities.Contracts.Evidence;
using BIMCapabilities.Contracts.Reports.Aggregation;

namespace BIMCapabilities.Contracts.Tests;

internal static class EvidenceAggregationTestData
{
    internal static EvidenceAggregation CreateDemoAggregation()
    {
        return new EvidenceAggregation
        {
            AggregationId = "aggregation-001",
            SourceCollectionId = "collection-001",
            ProfileId = "compliance-report-v1",
            Rule = new EvidenceAggregationRule
            {
                RuleId = "group-by-severity",
                Name = "Group By Severity",
                GroupBy = "Severity",
                Strategy = "GroupAndSummarize",
                Metadata = new Dictionary<string, string>
                {
                    ["profileType"] = "Compliance"
                }
            }
        };
    }

    internal static EvidenceStatistics CreateDemoStatistics()
    {
        return new EvidenceStatistics
        {
            TotalCount = 10,
            Counts = new Dictionary<string, int>
            {
                ["error"] = 3,
                ["warning"] = 2,
                ["info"] = 5
            },
            Totals = new Dictionary<string, int>
            {
                ["records"] = 10
            },
            Breakdowns = new Dictionary<string, IReadOnlyDictionary<string, int>>
            {
                ["severity"] = new Dictionary<string, int>
                {
                    ["error"] = 3,
                    ["warning"] = 2,
                    ["info"] = 5
                }
            },
            Percentages = new Dictionary<string, decimal>
            {
                ["error"] = 30.0m,
                ["warning"] = 20.0m,
                ["info"] = 50.0m
            }
        };
    }

    internal static EvidenceSummary CreateDemoSummary()
    {
        return new EvidenceSummary
        {
            TotalEvidence = 10,
            BySeverity = new Dictionary<EvidenceSeverity, int>
            {
                [EvidenceSeverity.Error] = 3,
                [EvidenceSeverity.Warning] = 2,
                [EvidenceSeverity.Info] = 5
            },
            ByCategory = new Dictionary<EvidenceCategory, int>
            {
                [EvidenceCategory.Validation] = 6,
                [EvidenceCategory.Compliance] = 4
            },
            BySource = new Dictionary<string, int>
            {
                ["parameter-engine"] = 6,
                ["naming-engine"] = 4
            },
            ByTarget = new Dictionary<string, int>
            {
                ["door-001"] = 5,
                ["window-001"] = 5
            },
            Statistics = CreateDemoStatistics()
        };
    }

    internal static EvidenceGroup CreateDemoGroup()
    {
        return new EvidenceGroup
        {
            GroupKey = "severity:error",
            GroupName = "Error",
            EvidenceReferences = ["evidence-001", "evidence-002", "evidence-003"],
            Summary = new EvidenceSummary
            {
                TotalEvidence = 3,
                BySeverity = new Dictionary<EvidenceSeverity, int>
                {
                    [EvidenceSeverity.Error] = 3
                }
            }
        };
    }

    internal static EvidenceAggregationResult CreateDemoResult()
    {
        return new EvidenceAggregationResult
        {
            AggregationId = "aggregation-001",
            Summary = CreateDemoSummary(),
            Groups = [CreateDemoGroup()],
            Statistics = CreateDemoStatistics()
        };
    }
}
