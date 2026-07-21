using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class ValidDoorFamiliesFixture
{
    internal static NamingTargetSet CreateTargetSet()
    {
        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-valid-door-families",
            families:
            [
                NamingFixtureBuilder.CreateFamily("family-001", "DR_SingleDoor", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-002", "DR_DoubleDoor", "category-doors", "Doors")
            ],
            familyTypes:
            [
                NamingFixtureBuilder.CreateFamilyType("family-type-001", "DR_SingleDoor900x2100"),
                NamingFixtureBuilder.CreateFamilyType("family-type-002", "DR_DoubleDoor1000x2100")
            ],
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "all-door-families",
                ["fixture"] = nameof(ValidDoorFamiliesFixture)
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateDoorComplianceRequest(CreateTargetSet());
    }
}
