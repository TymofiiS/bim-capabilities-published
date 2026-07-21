using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class LargeNamingFixture
{
    internal const int ValidFamilyCount = 50;
    internal const int InvalidFamilyCount = 50;

    internal static NamingTargetSet CreateTargetSet()
    {
        var families = new List<NormalizedFamily>(ValidFamilyCount + InvalidFamilyCount);

        for (var index = 0; index < ValidFamilyCount; index++)
        {
            families.Add(NamingFixtureBuilder.CreateFamily(
                $"family-valid-{index:D3}",
                $"DR_ValidDoor{index:D3}",
                "category-doors",
                "Doors"));
        }

        for (var index = 0; index < InvalidFamilyCount; index++)
        {
            families.Add(NamingFixtureBuilder.CreateFamily(
                $"family-invalid-{index:D3}",
                $"DoorInvalid{index:D3}",
                "category-doors",
                "Doors"));
        }

        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-large-naming",
            families,
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "large-door-families",
                ["fixture"] = nameof(LargeNamingFixture),
                ["familyCount"] = (ValidFamilyCount + InvalidFamilyCount).ToString()
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateDoorComplianceRequest(CreateTargetSet());
    }
}
