using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class MissingFireRatingFixture
{
    internal static ParameterWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-001", "HTL_Door_01");
        var targetSet = WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-missing-fire-rating", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateMissingParameterResult(
            "family-door-001",
            "HTL_Door_01",
            "FireRating");

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(complianceResult, targetSet);
    }
}
