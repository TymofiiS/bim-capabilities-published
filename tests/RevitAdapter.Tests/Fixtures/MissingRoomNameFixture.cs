using BIMCapabilities.Adapters.Revit.Tests.Builders;
using BIMCapabilities.Contracts.Engines.Parameter.Write;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures;

internal static class MissingRoomNameFixture
{
    internal static ParameterWriteRequestBuildRequest CreateBuildRequest()
    {
        var family = WriteLayerFixtureBuilder.CreateDoorFamily("family-door-002", "HTL_Door_02");
        var targetSet = WriteLayerFixtureBuilder.CreateParameterTargetSet("fixture-missing-room-name", family);
        var complianceResult = WriteLayerFixtureBuilder.CreateMissingParameterResult(
            "family-door-002",
            "HTL_Door_02",
            "RoomName");

        return WriteLayerFixtureBuilder.CreateParameterBuildRequest(complianceResult, targetSet);
    }
}
