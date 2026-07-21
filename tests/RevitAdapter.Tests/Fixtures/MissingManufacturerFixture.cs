using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class MissingManufacturerFixture
{
    internal static ParameterWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-003", "HTL_Door_03");
        var targetSet = WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-missing-manufacturer", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateMissingParameterResult(
            "family-door-003",
            "HTL_Door_03",
            "Manufacturer");

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(complianceResult, targetSet);
    }
}
