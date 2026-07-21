using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Contracts.Engines.Naming.Write;
using BIMCapabilities.Contracts.Engines.Parameter.Compliance;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class MixedIssuesFixture
{
    internal static ParameterWriteRequestBuildRequest CreateParameterBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-mixed-001", "Door_Mixed");
        var targetSet = WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-mixed-parameter", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateMissingParameterResult(
            "family-door-mixed-001",
            "Door_Mixed",
            "RoomName");

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(complianceResult, targetSet);
    }

    internal static NamingWriteRequestBuildRequest CreateNamingBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-mixed-001", "Door_Mixed");
        var targetSet = WriteLayerFixtureBuilder.CreateNamingTargetSet("fixture-mixed-naming", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateInvalidNameResult(
            "family-door-mixed-001",
            "Door_Mixed");

        return new NamingWriteRequestBuildRequest
        {
            ComplianceResult = complianceResult,
            TargetSet = targetSet,
            RequiredPrefixes = [WriteLayerFixtureBuilder.DoorPrefix],
            PatternRule = WriteLayerFixtureBuilder.CreateDoorPatternRule(),
            CorrectionIntents =
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-door-mixed-001",
                    ProposedName = "DR_DoorMixed"
                }
            ],
            PrefixFixScope = PrefixFixScope.All,
            RequestedAt = WriteLayerFixtureBuilder.RequestedAt,
            RuleId = WriteLayerFixtureBuilder.RuleId,
            CorrelationId = WriteLayerFixtureBuilder.CorrelationId
        };
    }

    internal static ParameterComplianceResult CreateCombinedParameterComplianceResult()
    {
        return new ParameterComplianceResult
        {
            EngineId = "parameter.compliance",
            Findings =
            [
                WriteLayerFixtureBuilder.CreateMissingParameterResult(
                    "family-door-mixed-001", "Door_Mixed", "RoomName").Findings![0]
            ]
        };
    }

    internal static NamingComplianceResult CreateCombinedNamingComplianceResult()
    {
        return new NamingComplianceResult
        {
            EngineId = "naming.compliance",
            Findings =
            [
                WriteLayerFixtureBuilder.CreateInvalidNameResult(
                    "family-door-mixed-001", "Door_Mixed").Findings![0]
            ]
        };
    }
}
