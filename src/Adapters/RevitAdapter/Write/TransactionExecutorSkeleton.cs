using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Skeleton transaction executor that returns deterministic stub responses.
/// </summary>
public sealed class TransactionExecutorSkeleton : ITransactionExecutor
{
    public TransactionResult Execute(TransactionRequest request)
    {
        ArgumentGuard.ThrowIfNull(request);

        return RevitWriteStubResponses.CreateTransactionResult(request);
    }

    public TransactionResult ExecuteBatch(TransactionBatch batch)
    {
        ArgumentGuard.ThrowIfNull(batch);

        return RevitWriteStubResponses.CreateTransactionBatchResult(batch);
    }
}
