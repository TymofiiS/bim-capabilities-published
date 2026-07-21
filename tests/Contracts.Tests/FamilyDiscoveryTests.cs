using System.Reflection;
using System.Text.Json;
using BIMCapabilities.Contracts.Engines.Family;
using BIMCapabilities.Contracts.Engines.Family.Discovery;

namespace BIMCapabilities.Contracts.Tests;

public class FamilyDiscoveryTests
{
    [Fact]
    public void Family_discovery_contracts_are_data_only_types()
    {
        var discoveryTypes = typeof(FamilyDiscoveryRequest).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(FamilyDiscoveryRequest).Namespace);

        Assert.All(discoveryTypes, type =>
        {
            if (type == typeof(IFamilyDiscoveryAtom))
            {
                return;
            }

            Assert.True(type.IsSealed);

            var customMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .Where(method => method.Name is not ("ToString" or "GetHashCode" or "Equals" or "<Clone>$"));

            Assert.Empty(customMethods);
        });
    }

    [Fact]
    public void FamilyDiscoveryRequest_and_result_can_be_constructed()
    {
        var request = new FamilyDiscoveryRequest
        {
            CategoryNames = ["Doors"],
            FamilyNames = ["HTL_Door_01"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-discovery-001"
        };

        var result = new FamilyDiscoveryResult
        {
            AtomId = DiscoverAllFamiliesAtomId,
            Families = [],
            FamilyTypes = [],
            Statistics = new FamilyDiscoveryStatistics
            {
                DiscoveredFamilies = 0,
                DiscoveredFamilyTypes = 0,
                ProviderRetrievedFamilies = 0
            },
            Diagnostics =
            [
                new FamilyEngineDiagnostic
                {
                    Code = "FamilyDiscovery.Completed",
                    Message = "Discovery completed.",
                    Severity = FamilyEngineDiagnosticSeverity.Information
                }
            ]
        };

        Assert.Equal(["Doors"], request.CategoryNames);
        Assert.Equal(DiscoverAllFamiliesAtomId, result.AtomId);
        Assert.Single(result.Diagnostics!);
    }

    [Fact]
    public void FamilyDiscoveryRequest_supports_json_round_trip_serialization()
    {
        var original = new FamilyDiscoveryRequest
        {
            CategoryNames = ["Doors", "Windows"],
            FamilyNames = ["HTL_Door_01"],
            FamilyTypeNames = ["HTL_Door_01_900x2100"],
            RuleId = "STD-ARC-OPENINGS-V01",
            CorrelationId = "corr-family-discovery-001"
        };

        var json = JsonSerializer.Serialize(original, FamilyEngineSerialization.Options);
        var roundTrip = JsonSerializer.Deserialize<FamilyDiscoveryRequest>(json, FamilyEngineSerialization.Options);

        Assert.NotNull(roundTrip);
        Assert.Equal(original.CategoryNames, roundTrip.CategoryNames);
        Assert.Equal(original.FamilyNames, roundTrip.FamilyNames);
        Assert.Equal(original.CorrelationId, roundTrip.CorrelationId);
    }

    [Fact]
    public void IFamilyDiscoveryAtom_defines_discovery_contract()
    {
        var method = Assert.Single(typeof(IFamilyDiscoveryAtom).GetMethods(), candidate => candidate.Name == "Discover");

        Assert.Equal(typeof(FamilyDiscoveryResult), method.ReturnType);
        Assert.Equal(typeof(FamilyDiscoveryRequest), method.GetParameters()[0].ParameterType);
        Assert.Equal(typeof(Adapters.Revit.Read.IFamilyProvider), method.GetParameters()[1].ParameterType);
    }

    private const string DiscoverAllFamiliesAtomId = "family.discovery.all";
}
