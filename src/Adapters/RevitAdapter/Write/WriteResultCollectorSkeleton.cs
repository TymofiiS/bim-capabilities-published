using BIMCapabilities.Contracts.Adapters.Revit.Write;

namespace BIMCapabilities.Adapters.Revit.Write;

/// <summary>
/// Skeleton result collector for write layer composition.
/// </summary>
public sealed class WriteResultCollectorSkeleton : IWriteResultCollector
{
    private readonly List<WriteRequestResult> _writeResults = [];
    private readonly List<TransactionResult> _transactionResults = [];

    public IReadOnlyList<WriteRequestResult> GetWriteResults()
    {
        return _writeResults.ToArray();
    }

    public IReadOnlyList<TransactionResult> GetTransactionResults()
    {
        return _transactionResults.ToArray();
    }

    public void Collect(WriteRequestResult result)
    {
        ArgumentGuard.ThrowIfNull(result);
        _writeResults.Add(result);
    }

    public void Collect(TransactionResult result)
    {
        ArgumentGuard.ThrowIfNull(result);
        _transactionResults.Add(result);
    }

    public void Clear()
    {
        _writeResults.Clear();
        _transactionResults.Clear();
    }
}
