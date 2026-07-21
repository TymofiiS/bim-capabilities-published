using BIMCapabilities.Adapters.Revit.Read;
using BIMCapabilities.Contracts.Adapters.Revit.Read;

namespace BIMCapabilities.Adapters.Revit.Tests.Fixtures.EndToEnd;

internal static class LargeFixture
{
    internal const int FamilyCount = 30;

    internal static RevitAdapter CreateAdapter() =>
        RevitAdapterEndToEndFixtureBuilder.CreateAdapter(EndToEndFixtureKind.Large);

    internal static RevitAdapterReadContext CreateContext() =>
        RevitAdapterEndToEndFixtureBuilder.CreateLargeDatasetScenarioContext();
}
