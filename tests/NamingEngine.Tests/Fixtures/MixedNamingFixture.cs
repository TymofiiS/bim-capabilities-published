using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class MixedNamingFixture
{
    internal static NamingTargetSet CreateTargetSet()
    {
        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-mixed-naming",
            families:
            [
                NamingFixtureBuilder.CreateFamily("family-401", "DR_SingleDoor", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-402", "Door_Single", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-403", "DR_DoubleDoor", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-404", "DR-", "category-doors", "Doors")
            ],
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "mixed-door-families",
                ["fixture"] = nameof(MixedNamingFixture)
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateDoorComplianceRequest(CreateTargetSet());
    }
}
