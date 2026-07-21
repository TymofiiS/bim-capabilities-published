using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures.EndToEnd;

internal static class MixedFamilyFixture
{
    internal static RevitAdapter CreateAdapter() =>
        RevitAdapterEndToEndFixtureBuilder.CreateAdapter();

    internal static RevitAdapterReadContext CreateContext() =>
        RevitAdapterEndToEndFixtureBuilder.CreateMixedFamilyScenarioContext();
}
