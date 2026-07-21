using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;

namespace BIMCapabilities.Engines.Naming.Tests.Builders;

/// <summary>
/// Builds deterministic naming target sets and compliance requests for MVP fixtures.
/// </summary>
internal static class NamingFixtureBuilder
{
    internal const string RuleId = "STD-ARC-OPENINGS-V01";
    internal const string DefaultCorrelationId = "corr-naming-e2e-001";
    internal const string DoorPrefix = "DR_";
    internal const string WindowPrefix = "WN_";

    internal static readonly DateTimeOffset ExecutedAt = new(2026, 6, 20, 12, 30, 0, TimeSpan.Zero);

    internal static NormalizedFamily CreateFamily(
        string id,
        string name,
        string categoryId = "category-generic",
        string categoryName = "Generic")
    {
        return new NormalizedFamily
        {
            Identity = new NormalizedIdentifier
            {
                Id = id,
                Kind = "family",
                Scope = "project-document"
            },
            Name = name,
            Category = new NormalizedCategory
            {
                Identifier = new NormalizedIdentifier { Id = categoryId, Kind = "category" },
                Name = categoryName
            }
        };
    }

    internal static NormalizedFamilyType CreateFamilyType(string id, string name)
    {
        return new NormalizedFamilyType
        {
            Identity = new NormalizedIdentifier { Id = id, Kind = "familyType" },
            Name = name
        };
    }

    internal static NamingTargetSet CreateTargetSet(
        string targetSetId,
        IReadOnlyList<NormalizedFamily> families,
        IReadOnlyList<NormalizedFamilyType>? familyTypes = null,
        IReadOnlyDictionary<string, string>? selectionMetadata = null)
    {
        return new NamingTargetSet
        {
            TargetSetId = targetSetId,
            TargetFamilies = families,
            TargetTypes = familyTypes,
            SelectionMetadata = selectionMetadata ?? new Dictionary<string, string>
            {
                ["fixtureSource"] = "naming-engine-e2e",
                ["ruleId"] = RuleId
            }
        };
    }

    internal static NamingPatternRule CreateDoorPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "DR_{Token}",
            RegularExpression = @"^DR_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    internal static NamingPatternRule CreateWindowPatternRule()
    {
        return new NamingPatternRule
        {
            TokenizedPattern = "WN_{Token}",
            RegularExpression = @"^WN_[A-Za-z][A-Za-z0-9]*$",
            AllowedCharacters = "A-Za-z0-9_",
            ForbiddenCharacters = [" ", "-"]
        };
    }

    internal static NamingComplianceRequest CreateDoorComplianceRequest(NamingTargetSet targetSet)
    {
        return CreateComplianceRequest(
            targetSet,
            requiredPrefixes: [DoorPrefix],
            patternRule: CreateDoorPatternRule());
    }

    internal static NamingComplianceRequest CreateWindowComplianceRequest(NamingTargetSet targetSet)
    {
        return CreateComplianceRequest(
            targetSet,
            requiredPrefixes: [WindowPrefix],
            patternRule: CreateWindowPatternRule());
    }

    internal static NamingComplianceRequest CreateComplianceRequest(
        NamingTargetSet targetSet,
        IReadOnlyList<string>? requiredPrefixes,
        NamingPatternRule? patternRule,
        string? correlationId = null)
    {
        return new NamingComplianceRequest
        {
            TargetSet = targetSet,
            RequiredPrefixes = requiredPrefixes,
            PatternRule = patternRule,
            ExecutedAt = ExecutedAt,
            RuleId = RuleId,
            CorrelationId = correlationId ?? DefaultCorrelationId
        };
    }
}
