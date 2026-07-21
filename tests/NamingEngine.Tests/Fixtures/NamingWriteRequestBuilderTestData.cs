using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Pattern;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Engines.Naming.Tests.Builders;
using BIMCapabilities.Engines.Naming.Tests.Fixtures;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class NamingWriteRequestBuilderTestData
{
    internal const string CorrelationId = "corr-naming-write-builder-001";
    internal static readonly DateTimeOffset RequestedAt = new(2026, 6, 20, 18, 0, 0, TimeSpan.Zero);

    internal static NamingWriteRequestBuildRequest CreateDoorBuildRequest(
        NamingComplianceResult complianceResult,
        NamingTargetSet? targetSet = null,
        IReadOnlyList<NamingWriteCorrectionIntent>? correctionIntents = null)
    {
        return CreateBuildRequest(
            complianceResult,
            targetSet ?? InvalidDoorFamiliesFixture.CreateTargetSet(),
            requiredPrefixes: [NamingFixtureBuilder.DoorPrefix],
            patternRule: NamingFixtureBuilder.CreateDoorPatternRule(),
            correctionIntents);
    }

    internal static NamingWriteRequestBuildRequest CreateWindowBuildRequest(
        NamingComplianceResult complianceResult,
        NamingTargetSet? targetSet = null,
        IReadOnlyList<NamingWriteCorrectionIntent>? correctionIntents = null)
    {
        return CreateBuildRequest(
            complianceResult,
            targetSet ?? InvalidWindowFamiliesFixture.CreateTargetSet(),
            requiredPrefixes: [NamingFixtureBuilder.WindowPrefix],
            patternRule: NamingFixtureBuilder.CreateWindowPatternRule(),
            correctionIntents);
    }

    internal static NamingWriteRequestBuildRequest CreateBuildRequest(
        NamingComplianceResult complianceResult,
        NamingTargetSet targetSet,
        IReadOnlyList<string> requiredPrefixes,
        NamingPatternRule patternRule,
        IReadOnlyList<NamingWriteCorrectionIntent>? correctionIntents = null)
    {
        return new NamingWriteRequestBuildRequest
        {
            ComplianceResult = complianceResult,
            TargetSet = targetSet,
            RequiredPrefixes = requiredPrefixes,
            PatternRule = patternRule,
            CorrectionIntents = correctionIntents,
            PrefixFixScope = PrefixFixScope.All,
            RequestedAt = RequestedAt,
            RuleId = NamingFixtureBuilder.RuleId,
            CorrelationId = CorrelationId
        };
    }

    internal static NamingComplianceResult CreateFailedFamilyFinding(
        string objectId,
        string objectKind,
        string objectName,
        string validationStage,
        string status)
    {
        return new NamingComplianceResult
        {
            EngineId = "naming.compliance",
            Findings =
            [
                new NamingComplianceFinding
                {
                    ValidationStage = validationStage,
                    ObjectId = objectId,
                    ObjectKind = objectKind,
                    ObjectName = objectName,
                    Passed = false,
                    Status = status,
                    Message = $"Naming validation failed for '{objectName}'."
                }
            ]
        };
    }
}
