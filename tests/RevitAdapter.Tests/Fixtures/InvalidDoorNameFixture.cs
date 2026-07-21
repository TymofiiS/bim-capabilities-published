using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class InvalidDoorNameFixture
{
    internal static NamingWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-004", "Door_01");
        var targetSet = WriteLayerFixtureBuilder.CreateNamingTargetSet("fixture-invalid-door-name", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateInvalidNameResult(
            "family-door-004",
            "Door_01");

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
                    ObjectId = "family-door-004",
                    ProposedName = "DR_Door_01"
                }
            ],
            PrefixFixScope = PrefixFixScope.All,
            RequestedAt = WriteLayerFixtureBuilder.RequestedAt,
            RuleId = WriteLayerFixtureBuilder.RuleId,
            CorrelationId = WriteLayerFixtureBuilder.CorrelationId
        };
    }
}
