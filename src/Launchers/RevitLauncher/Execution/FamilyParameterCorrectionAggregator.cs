using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Launchers.Revit.Execution;

internal static class FamilyParameterCorrectionAggregator
{
    internal static void AddOrUpdateParameter(
        IDictionary<string, ParameterCorrectionState> parameters,
        string parameterName,
        string? typeName,
        string requestedValue,
        bool isInstance,
        WriteRequest writeRequest,
        ICollection<WriteRequestReference> sourceRequests)
    {
        if (!parameters.TryGetValue(parameterName, out var correction))
        {
            correction = new ParameterCorrectionState
            {
                ParameterName = parameterName,
                SampleRequest = writeRequest
            };
            parameters[parameterName] = correction;
        }

        correction.IsInstance = isInstance;

        if (!string.IsNullOrWhiteSpace(typeName))
        {
            correction.ValuesByTypeName[typeName] = requestedValue;
        }
        else
        {
            correction.RequestedValue = requestedValue;
        }

        sourceRequests.Add(new WriteRequestReference
        {
            RequestId = writeRequest.RequestId,
            RequestType = writeRequest.RequestType,
            Status = WriteRequestStatus.Succeeded,
            Order = writeRequest.Order
        });
    }

    internal static string? ResolveValueForType(ParameterCorrectionState correction, string familyTypeName)
    {
        if (correction.ValuesByTypeName.Count > 0)
        {
            return correction.ValuesByTypeName.TryGetValue(familyTypeName, out var typeValue)
                ? typeValue
                : null;
        }

        return string.IsNullOrWhiteSpace(correction.RequestedValue)
            ? null
            : correction.RequestedValue;
    }

    internal sealed class ParameterCorrectionState
    {
        public required string ParameterName { get; init; }

        public string RequestedValue { get; set; } = string.Empty;

        public Dictionary<string, string> ValuesByTypeName { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public bool IsInstance { get; set; }

        public required WriteRequest SampleRequest { get; init; }
    }
}
