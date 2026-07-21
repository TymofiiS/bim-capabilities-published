using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class ValidWindowFamiliesFixture
{
    internal static NamingTargetSet CreateTargetSet()
    {
        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-valid-window-families",
            families:
            [
                NamingFixtureBuilder.CreateFamily("family-201", "WN_Window01", "category-windows", "Windows"),
                NamingFixtureBuilder.CreateFamily("family-202", "WN_Window02", "category-windows", "Windows")
            ],
            familyTypes:
            [
                NamingFixtureBuilder.CreateFamilyType("family-type-201", "WN_Window011200x1200"),
                NamingFixtureBuilder.CreateFamilyType("family-type-202", "WN_Window021500x1200")
            ],
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "all-window-families",
                ["fixture"] = nameof(ValidWindowFamiliesFixture)
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateWindowComplianceRequest(CreateTargetSet());
    }
}
