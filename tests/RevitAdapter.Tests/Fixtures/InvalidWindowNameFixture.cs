using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Naming.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class InvalidWindowNameFixture
{
    internal static NamingWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateWindowFamily("family-window-002", "Window_01");
        var targetSet = WriteLayerFixtureBuilder.CreateNamingTargetSet("fixture-invalid-window-name", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateInvalidNameResult(
            "family-window-002",
            "Window_01");

        return new NamingWriteRequestBuildRequest
        {
            ComplianceResult = complianceResult,
            TargetSet = targetSet,
            RequiredPrefixes = [WriteLayerFixtureBuilder.WindowPrefix],
            PatternRule = WriteLayerFixtureBuilder.CreateWindowPatternRule(),
            CorrectionIntents =
            [
                new NamingWriteCorrectionIntent
                {
                    ObjectId = "family-window-002",
                    ProposedName = "WN_Window_01"
                }
            ],
            PrefixFixScope = PrefixFixScope.All,
            RequestedAt = WriteLayerFixtureBuilder.RequestedAt,
            RuleId = WriteLayerFixtureBuilder.RuleId,
            CorrelationId = WriteLayerFixtureBuilder.CorrelationId
        };
    }
}
