namespace BIMCapabilities.Contracts.Adapters.Revit.Write;

/// <summary>
/// Execution status for a write request or batch.
/// </summary>
public enum WriteRequestStatus
{
    Succeeded,
    Failed,
    PartiallySucceeded,
    Skipped,
    NotExecuted
}
