using BIMCapabilities.Contracts.Adapters.Revit.Translation;
using BIMCapabilities.Contracts.Adapters.Revit.Write;
using BIMCapabilities.Launchers.Revit.Execution;

namespace BIMCapabilities.Launchers.Revit.Tests;

public class FamilyParameterCorrectionAggregatorTests
{
    [Fact]
    public void Aggregator_preserves_per_type_values_for_same_parameter()
    {
        var parameters = new Dictionary<string, FamilyParameterCorrectionAggregator.ParameterCorrectionState>(
            StringComparer.OrdinalIgnoreCase);
        var sourceRequests = new List<WriteRequestReference>();

        AddTypeCorrection(parameters, sourceRequests, "Model", "750 x 2000mm", "750 x 2000mm");
        AddTypeCorrection(parameters, sourceRequests, "Model", "750 x 2100mm", "750 x 2100mm");
        AddTypeCorrection(parameters, sourceRequests, "Model", "900 x 2000mm", "900 x 2000mm");
        AddTypeCorrection(parameters, sourceRequests, "Model", "900 x 2100mm", "900 x 2100mm");

        var correction = Assert.Single(parameters.Values);
        Assert.Equal(4, correction.ValuesByTypeName.Count);
        Assert.Equal("750 x 2100mm", FamilyParameterCorrectionAggregator.ResolveValueForType(correction, "750 x 2100mm"));
        Assert.Equal("900 x 2100mm", FamilyParameterCorrectionAggregator.ResolveValueForType(correction, "900 x 2100mm"));
        Assert.Null(FamilyParameterCorrectionAggregator.ResolveValueForType(correction, "600 x 2000mm"));
        Assert.Equal(4, sourceRequests.Count);
    }

    [Fact]
    public void Aggregator_uses_family_level_value_when_no_type_specific_values_exist()
    {
        var parameters = new Dictionary<string, FamilyParameterCorrectionAggregator.ParameterCorrectionState>(
            StringComparer.OrdinalIgnoreCase);
        var sourceRequests = new List<WriteRequestReference>();

        FamilyParameterCorrectionAggregator.AddOrUpdateParameter(
            parameters,
            "FireRating",
            typeName: null,
            requestedValue: "60 min",
            isInstance: false,
            writeRequest: CreateWriteRequest("family-001", "FireRating", "60 min"),
            sourceRequests);

        var correction = Assert.Single(parameters.Values);
        Assert.Empty(correction.ValuesByTypeName);
        Assert.Equal("60 min", FamilyParameterCorrectionAggregator.ResolveValueForType(correction, "Any Type"));
    }

    private static void AddTypeCorrection(
        IDictionary<string, FamilyParameterCorrectionAggregator.ParameterCorrectionState> parameters,
        ICollection<WriteRequestReference> sourceRequests,
        string parameterName,
        string typeName,
        string requestedValue)
    {
        FamilyParameterCorrectionAggregator.AddOrUpdateParameter(
            parameters,
            parameterName,
            typeName,
            requestedValue,
            isInstance: false,
            writeRequest: CreateWriteRequest($"type-{typeName}", parameterName, requestedValue),
            sourceRequests);
    }

    private static WriteRequest CreateWriteRequest(string targetId, string parameterName, string requestedValue)
    {
        return new WriteRequest
        {
            RequestId = $"parameter-write-update-{targetId}-{parameterName}",
            TargetObject = new NormalizedIdentifier
            {
                Id = targetId,
                Kind = "familyType",
                Scope = "project-document"
            },
            RequestType = WriteRequestType.ParameterUpdate,
            Order = 1,
            Payload = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["parameterName"] = parameterName,
                ["requestedValue"] = requestedValue,
                ["parameterIsInstance"] = "false"
            }
        };
    }
}
