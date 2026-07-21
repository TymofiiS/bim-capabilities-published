using BIMCapabilities.Contracts.Engines.Naming;
using BIMCapabilities.Contracts.Engines.Naming.Compliance;
using BIMCapabilities.Engines.Naming.Tests.Builders;

namespace BIMCapabilities.Engines.Naming.Tests.Fixtures;

internal static class InvalidWindowFamiliesFixture
{
    internal static NamingTargetSet CreateTargetSet()
    {
        return NamingFixtureBuilder.CreateTargetSet(
            "fixture-invalid-window-families",
            families:
            [
                NamingFixtureBuilder.CreateFamily("family-301", "Window_01", "category-windows", "Windows"),
                NamingFixtureBuilder.CreateFamily("family-302", "WN-", "category-windows", "Windows"),
                NamingFixtureBuilder.CreateFamily("family-303", "WN Window", "category-windows", "Windows")
            ],
            selectionMetadata: new Dictionary<string, string>
            {
                ["scope"] = "all-window-families",
                ["fixture"] = nameof(InvalidWindowFamiliesFixture)
            });
    }

    internal static NamingComplianceRequest CreateComplianceRequest()
    {
        return NamingFixtureBuilder.CreateWindowComplianceRequest(CreateTargetSet());
    }
}
