using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Skeleton write request executor that returns deterministic stub responses.
/// </summary>
public sealed class WriteRequestExecutorSkeleton : IWriteRequestExecutor
{
    public WriteRequestResult Execute(WriteRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        return RevitWriteStubResponses.CreateWriteRequestResult(request);
    }

    public WriteRequestResult ExecuteBatch(WriteRequestBatch batch)
    {
        ArgumentGuard.ThrowIfNull(batch);

        return RevitWriteStubResponses.CreateWriteRequestBatchResult(batch);
    }
}
