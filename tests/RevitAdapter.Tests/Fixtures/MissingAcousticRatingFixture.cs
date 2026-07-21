using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class MissingAcousticRatingFixture
{
    internal static ParameterWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateWindowFamily("family-window-001", "HTL_Window_01");
        var targetSet = WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-missing-acoustic-rating", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateMissingParameterResult(
            "family-window-001",
            "HTL_Window_01",
            "AcousticRating");

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(complianceResult, targetSet);
    }
}
