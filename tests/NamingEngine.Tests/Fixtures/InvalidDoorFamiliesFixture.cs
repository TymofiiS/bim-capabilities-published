using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class InvalidDoorFamiliesFixture
{
    internal static NamingTargetSet CreateTargetSet()
    {
        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-invalid-door-families",
            families:
            [
                NamingFixtureBuilder.CreateFamily("family-101", "Door_Single", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-102", "DR-", "category-doors", "Doors"),
                NamingFixtureBuilder.CreateFamily("family-103", "DR Door", "category-doors", "Doors")
            ],
            familyTypes:
            [
                NamingFixtureBuilder.CreateFamilyType("family-type-101", "HTL_Door_Invalid")
            ],
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "all-door-families",
                ["fixture"] = nameof(InvalidDoorFamiliesFixture)
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateDoorComplianceRequest(CreateTargetSet());
    }
}
